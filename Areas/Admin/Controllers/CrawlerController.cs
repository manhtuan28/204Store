using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _204Store.Data;
using _204Store.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Globalization;

namespace _204Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CrawlerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CrawlerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RunCrawlUrl(string url)
        {
            var results = new List<CrawlResultViewModel>();
            if (string.IsNullOrEmpty(url)) return Json(new { success = false, message = "URL trống" });

            try
            {
                // 1. TẠO DANH MỤC TỰ ĐỘNG
                var categoryInfo = ParseCategoryFromUrl(url);
                var parentCategory = await GetOrCreateCategory(categoryInfo.ParentName);
                var childCategory = await GetOrCreateCategory(categoryInfo.ChildName, parentCategory.Id);

                // Cấu hình Driver
                var options = new ChromeOptions();
                options.AddArgument("--headless");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--no-sandbox");
                options.AddArgument("--window-size=1920,1080");
                options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                using (var driver = new ChromeDriver(options))
                {
                    // Hàng chờ (Queue) các Link cần quét
                    Queue<string> urlsToVisit = new Queue<string>();
                    HashSet<string> visitedUrls = new HashSet<string>(); // Tránh trùng lặp

                    // TH1: Nếu là link danh mục (chứa /mobile/, /laptop/...) -> Lấy danh sách sp
                    if (url.Contains(".html") == false || url.Contains("/mobile/") || url.Contains("/laptop/"))
                    {
                        driver.Navigate().GoToUrl(url);
                        Thread.Sleep(2000);
                        ((IJavaScriptExecutor)driver).ExecuteScript("window.scrollBy(0, 1000);");
                        Thread.Sleep(1000);

                        try
                        {
                            var links = driver.FindElements(By.CssSelector("a.product__link"))
                                              .Select(x => x.GetAttribute("href"))
                                              .Distinct().Take(6).ToList(); // Lấy demo 6 cái
                            foreach (var l in links) urlsToVisit.Enqueue(l);
                        }
                        catch { }
                    }
                    else
                    {
                        // TH2: Nếu là link chi tiết 1 sản phẩm -> Thêm trực tiếp vào hàng chờ
                        urlsToVisit.Enqueue(url);
                    }

                    // VÒNG LẶP QUÉT URL (Bao gồm cả các phiên bản anh em tìm thấy)
                    while (urlsToVisit.Count > 0)
                    {
                        string currentUrl = urlsToVisit.Dequeue();

                        // Nếu đã quét rồi hoặc url rỗng thì bỏ qua
                        if (string.IsNullOrEmpty(currentUrl) || visitedUrls.Contains(currentUrl)) continue;
                        visitedUrls.Add(currentUrl);

                        try
                        {
                            driver.Navigate().GoToUrl(currentUrl);
                            Thread.Sleep(1500);

                            // --- 1. TÌM CÁC PHIÊN BẢN KHÁC (SIBLINGS) ---
                            // Đây là bước quan trọng để lấy đủ 256GB, 512GB, 1TB...
                            try
                            {
                                var siblingLinks = driver.FindElements(By.CssSelector(".list-linked .item-linked"))
                                                         .Select(x => x.GetAttribute("href"))
                                                         .Where(x => !string.IsNullOrEmpty(x) && x.Contains("http"))
                                                         .ToList();

                                foreach (var sibling in siblingLinks)
                                {
                                    // Nếu link chưa có trong hàng chờ và chưa quét -> Thêm vào để quét tiếp
                                    if (!visitedUrls.Contains(sibling) && !urlsToVisit.Contains(sibling))
                                    {
                                        urlsToVisit.Enqueue(sibling);
                                    }
                                }
                            }
                            catch { }

                            // --- 2. LẤY DỮ LIỆU CƠ BẢN ---
                            var nameEl = driver.FindElements(By.CssSelector(".box-name__box-product-name h1")).FirstOrDefault()
                                      ?? driver.FindElements(By.TagName("h1")).FirstOrDefault();
                            if (nameEl == null) continue;
                            string rawName = nameEl.Text.Trim();

                            // --- 3. LẤY & LÀM SẠCH ẢNH (YÊU CẦU MỚI) ---
                            var galleryImages = new List<string>();
                            try
                            {
                                // Chỉ lấy thẻ img bên trong .button__view-gallery (để loại bỏ video)
                                var imgElements = driver.FindElements(By.CssSelector(".swiper-slide.button__view-gallery img"));
                                foreach (var imgEl in imgElements)
                                {
                                    string src = imgEl.GetAttribute("src");
                                    if (string.IsNullOrEmpty(src) || !src.StartsWith("http"))
                                        src = imgEl.GetAttribute("data-src");

                                    // HÀM LÀM SẠCH LINK ẢNH
                                    string cleanSrc = CleanCellphonesImage(src);

                                    if (!string.IsNullOrEmpty(cleanSrc) && !galleryImages.Contains(cleanSrc))
                                        galleryImages.Add(cleanSrc);
                                }
                            }
                            catch { }

                            string mainImage = galleryImages.FirstOrDefault() ?? "";
                            var secondaryImages = galleryImages.Skip(1).ToList();

                            // --- 4. LẤY THÔNG SỐ (JSON) ---
                            string jsonSpecs = "[]";
                            string screen = "";
                            string cpu = "";
                            try
                            {
                                var specRows = driver.FindElements(By.CssSelector(".technical-content .technical-content-item"));
                                var specList = new List<object>();
                                foreach (var row in specRows)
                                {
                                    var cells = row.FindElements(By.TagName("td"));
                                    if (cells.Count >= 2)
                                    {
                                        string k = cells[0].Text.Trim();
                                        string v = cells[1].Text.Trim();
                                        specList.Add(new { Key = k, Value = v });
                                        if (k.Contains("Kích thước")) screen = v;
                                        if (k.Contains("CPU") || k.Contains("Chip")) cpu = v;
                                    }
                                }
                                jsonSpecs = JsonConvert.SerializeObject(specList);
                            }
                            catch { }

                            // --- 5. XỬ LÝ BIẾN THỂ ---
                            var variantEls = driver.FindElements(By.CssSelector(".list-variants .item-variant"));

                            // Xác định ROM hiện tại của link này (Dựa vào class active)
                            string currentRom = "Tiêu chuẩn";
                            try
                            {
                                var activeVer = driver.FindElements(By.CssSelector(".list-linked .item-linked.active strong")).FirstOrDefault();
                                if (activeVer != null) currentRom = activeVer.Text.Trim();
                                else
                                {
                                    // Nếu không có bảng version, parse từ tên
                                    var (r, romParsed) = ParseRamRom(rawName, categoryInfo.ParentName);
                                    currentRom = romParsed;
                                }
                            }
                            catch { }

                            // Lấy RAM
                            string currentRam = "8GB";
                            var (parsedRam, parsedRom) = ParseRamRom(rawName, categoryInfo.ParentName);
                            if (parsedRam != "8GB") currentRam = parsedRam;

                            // TH1: Sản phẩm đơn (Laptop)
                            if (variantEls.Count == 0)
                            {
                                string priceTxt = "0";
                                var priceEl = driver.FindElements(By.CssSelector(".product__price--show")).FirstOrDefault();
                                if (priceEl != null) priceTxt = priceEl.Text;

                                await SaveProductAndDetail(rawName, childCategory.Id, screen, cpu, jsonSpecs,
                                                           mainImage, secondaryImages,
                                                           "Tiêu chuẩn", currentRam, currentRom, priceTxt, results);
                            }
                            // TH2: Sản phẩm nhiều màu (Điện thoại)
                            else
                            {
                                foreach (var v in variantEls)
                                {
                                    try
                                    {
                                        string color = v.FindElement(By.CssSelector(".item-variant-name")).Text.Trim();
                                        string pText = v.FindElement(By.CssSelector(".item-variant-price")).Text;

                                        string vImg = CleanCellphonesImage(v.FindElement(By.TagName("img")).GetAttribute("src"));
                                        if (string.IsNullOrEmpty(vImg)) vImg = mainImage;

                                        await SaveProductAndDetail(rawName, childCategory.Id, screen, cpu, jsonSpecs,
                                                                   vImg, secondaryImages,
                                                                   color, currentRam, currentRom, pText, results);
                                    }
                                    catch { continue; }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Lỗi link {currentUrl}: {ex.Message}");
                        }
                    } // Kết thúc While
                }
                return Json(new { success = true, data = results });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // --- HÀM LÀM SẠCH LINK ẢNH (MỚI) ---
        private string CleanCellphonesImage(string url)
        {
            if (string.IsNullOrEmpty(url)) return "";

            // Input: https://cdn2.cellphones.com.vn/insecure/.../plain/https://cellphones.com.vn/...jpg
            // Output: https://cellphones.com.vn/...jpg
            if (url.Contains("/plain/"))
            {
                int index = url.IndexOf("/plain/");
                // Lấy phần tử sau chữ 'plain/' (độ dài là 7)
                return url.Substring(index + 7);
            }
            return url;
        }

        private (string ParentName, string ChildName) ParseCategoryFromUrl(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                string path = uri.AbsolutePath;
                var segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                string parent = "Khác";
                string child = "Chung";

                if (segments.Length >= 1)
                {
                    string p = segments[0].ToLower();
                    if (p.Contains("mobile") || p.Contains("dien-thoai")) parent = "Điện thoại";
                    else if (p.Contains("laptop")) parent = "Laptop";
                    else if (p.Contains("tablet")) parent = "Tablet";
                    else if (p.Contains("watch")) parent = "Đồng hồ";
                    else parent = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(p);
                }

                if (segments.Length >= 2)
                {
                    string c = segments[1].Replace(".html", "");
                    child = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(c);
                }

                return (parent, child);
            }
            catch
            {
                return ("Điện thoại", "Apple");
            }
        }

        private async Task<Category> GetOrCreateCategory(string name, int? parentId = null)
        {
            var cat = await _context.Categories.FirstOrDefaultAsync(c => c.Name == name && c.ParentId == parentId);
            if (cat == null)
            {
                cat = new Category
                {
                    Name = name,
                    Alias = name.ToLower().Replace(" ", "-"),
                    IsActive = true,
                    ParentId = parentId,
                    Icon = parentId == null ? "bi bi-grid" : ""
                };
                _context.Categories.Add(cat);
                await _context.SaveChangesAsync();
            }
            return cat;
        }

        private (string ram, string rom) ParseRamRom(string name, string cat)
        {
            string ram = "8GB";
            string rom = "256GB";
            name = name.ToUpper();

            var ramMatch = Regex.Match(name, @"(\d+)(GB|TB)\s*RAM");
            if (!ramMatch.Success) ramMatch = Regex.Match(name, @"RAM\s*(\d+)(GB|TB)");
            if (!ramMatch.Success) ramMatch = Regex.Match(name, @"(\d+)\s*GB");

            if (ramMatch.Success)
            {
                string valStr = Regex.Match(ramMatch.Value, @"\d+").Value;
                int val = int.Parse(valStr);
                if (val <= 64) ram = val + "GB";
            }

            var romMatch = Regex.Match(name, @"(\d+)(GB|TB)\s*(SSD|HDD)");
            if (!romMatch.Success)
            {
                var matches = Regex.Matches(name, @"(\d+)(GB|TB)");
                foreach (Match m in matches)
                {
                    string valStr = Regex.Match(m.Value, @"\d+").Value;
                    int val = int.Parse(valStr);
                    if (m.Value.Contains("TB") || val >= 128)
                    {
                        rom = m.Value;
                        break;
                    }
                }
            }
            else
            {
                rom = romMatch.Value;
            }
            return (ram, rom);
        }

        private async Task SaveProductAndDetail(string rawName, int catId, string screen, string cpu, string specs,
                                                string mainImg, List<string> secondaryImages,
                                                string color, string ram, string rom, string priceText,
                                                List<CrawlResultViewModel> results)
        {
            // Làm sạch tên: Bỏ dung lượng để gom nhóm Product Cha
            // Ví dụ: iPhone 17 Pro Max 256GB -> iPhone 17 Pro Max
            // Điều này cực quan trọng để các phiên bản 512GB, 1TB gom chung vào 1 sản phẩm
            string cleanName = Regex.Replace(rawName, @"\d+(GB|TB)", "", RegexOptions.IgnoreCase)
                                    .Replace("SSD", "").Replace("RAM", "")
                                    .Replace("|", "").Trim();
            while (cleanName.Contains("  ")) cleanName = cleanName.Replace("  ", " ");

            decimal price = 0;
            decimal.TryParse(Regex.Replace(priceText, @"[^\d]", ""), out price);
            if (price == 0) price = 10000000;

            string status = "Mới";
            var product = await _context.Products.Include(p => p.ProductImages).FirstOrDefaultAsync(p => p.Name == cleanName);

            // 1. LƯU PRODUCT CHA
            if (product == null)
            {
                product = new Product
                {
                    Name = cleanName,
                    CategoryId = catId,
                    Description = $"{cpu} / {screen}",
                    Specifications = specs,
                    ImageUrl = mainImg,
                    IsActive = true,
                    IsFeatured = true
                };
                _context.Products.Add(product);
                await _context.SaveChangesAsync();
            }
            else
            {
                status = "Cập nhật";
                // Nếu sản phẩm cha chưa có ảnh hoặc ảnh lỗi, update lại
                if (string.IsNullOrEmpty(product.ImageUrl)) product.ImageUrl = mainImg;
            }

            // 2. LƯU ẢNH PHỤ
            if (secondaryImages != null && secondaryImages.Any())
            {
                foreach (var imgUrl in secondaryImages)
                {
                    bool exists = await _context.ProductImages.AnyAsync(pi => pi.ProductId == product.Id && pi.Url == imgUrl);
                    if (!exists)
                    {
                        _context.ProductImages.Add(new ProductImage { ProductId = product.Id, Url = imgUrl });
                    }
                }
                await _context.SaveChangesAsync();
            }

            // 3. LƯU BIẾN THỂ (DETAIL)
            var detail = await _context.ProductDetails.FirstOrDefaultAsync(d => d.ProductId == product.Id && d.Rom == rom && d.Color == color);
            if (detail == null)
            {
                detail = new ProductDetail
                {
                    ProductId = product.Id,
                    Color = color,
                    Ram = ram,
                    Rom = rom,
                    Price = price,
                    Quantity = 10,
                    ImageUrl = mainImg,
                    IsActive = true
                };
                _context.ProductDetails.Add(detail);
            }
            else
            {
                detail.Price = price;
            }
            await _context.SaveChangesAsync();

            // Hiển thị ra View
            results.Add(new CrawlResultViewModel
            {
                Name = cleanName,
                Price = price,
                Image = mainImg,
                Ram = ram,
                Rom = rom + " (" + color + ")",
                Status = status
            });
        }
    }

    public class CrawlResultViewModel
    {
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Image { get; set; }
        public string Ram { get; set; }
        public string Rom { get; set; }
        public string Status { get; set; }
    }
}