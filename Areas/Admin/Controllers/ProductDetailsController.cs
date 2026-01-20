using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _204Store.Data;
using _204Store.Models;
using OfficeOpenXml;
using Newtonsoft.Json;

namespace _204Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductDetailsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductDetailsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<IActionResult> Index(int productId)
        {
            if (productId == 0) return NotFound();
            var product = await _context.Products.FindAsync(productId);
            if (product == null) return NotFound();
            var details = await _context.ProductDetails.Where(x => x.ProductId == productId).OrderBy(x => x.Id).ToListAsync();
            ViewBag.Product = product;
            return View(details);
        }

        public IActionResult Create(int productId)
        {
            var product = _context.Products.Find(productId);
            if (product == null) return NotFound();
            ViewBag.Product = product;
            return View(new ProductDetail { ProductId = productId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductDetail model, IFormFile? imageFile)
        {
            ModelState.Remove("Product");

            if (ModelState.IsValid)
            {
                if (imageFile != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                    string uploadFolder = Path.Combine(_environment.WebRootPath, "images", "variants");

                    if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                    string filePath = Path.Combine(uploadFolder, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(fileStream);
                    }
                    model.ImageUrl = "/images/variants/" + fileName;
                }

                _context.Add(model);
                await _context.SaveChangesAsync();
                TempData["success"] = "Thêm biến thể thành công";
                return RedirectToAction(nameof(Index), new { productId = model.ProductId });
            }

            ViewBag.Product = _context.Products.Find(model.ProductId);
            return View(model);
        }
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var detail = await _context.ProductDetails.Include(x => x.Product).FirstOrDefaultAsync(x => x.Id == id);
            if (detail == null) return NotFound();
            return View(detail);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductDetail model, IFormFile? imageFile)
        {
            if (id != model.Id) return NotFound();

            ModelState.Remove("Product");

            if (ModelState.IsValid)
            {
                try
                {
                    if (imageFile != null)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(imageFile.FileName);
                        string uploadFolder = Path.Combine(_environment.WebRootPath, "images", "variants");

                        if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                        string filePath = Path.Combine(uploadFolder, fileName);
                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(fileStream);
                        }
                        model.ImageUrl = "/images/variants/" + fileName;
                    }
                    else
                    {
                        var existingDetail = await _context.ProductDetails.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                        if (existingDetail != null)
                        {
                            model.ImageUrl = existingDetail.ImageUrl;
                        }
                    }

                    _context.Update(model);
                    await _context.SaveChangesAsync();
                    TempData["success"] = "Cập nhật thành công";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.ProductDetails.Any(e => e.Id == model.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index), new { productId = model.ProductId });
            }

            if (model.Product == null)
            {
                model.Product = await _context.Products.FindAsync(model.ProductId);
            }
            return View(model);
        }
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var detail = await _context.ProductDetails
                .Include(p => p.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (detail == null) return NotFound();

            return View(detail);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detail = await _context.ProductDetails.FindAsync(id);
            if (detail != null)
            {
                _context.ProductDetails.Remove(detail);
                await _context.SaveChangesAsync();
                TempData["success"] = "Đã xóa biến thể thành công";
                return RedirectToAction(nameof(Index), new { productId = detail.ProductId });
            }
            return NotFound();
        }

        [HttpGet]
        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length <= 0)
            {
                TempData["error"] = "Vui lòng chọn file Excel.";
                return View();
            }

            if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                TempData["error"] = "Chỉ hỗ trợ file .xlsx";
                return View();
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await file.CopyToAsync(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0];
                        var rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            var productName = worksheet.Cells[row, 1].Value?.ToString().Trim();
                            if (string.IsNullOrEmpty(productName)) continue;

                            var categoryName = worksheet.Cells[row, 2].Value?.ToString().Trim();
                            var description = worksheet.Cells[row, 3].Value?.ToString().Trim();

                            var rawSpecs = worksheet.Cells[row, 4].Value?.ToString().Trim();
                            string jsonSpecs = "[]";

                            if (!string.IsNullOrEmpty(rawSpecs))
                            {
                                var specList = new List<object>();
                                var pairs = rawSpecs.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                                foreach (var pair in pairs)
                                {
                                    var parts = pair.Split(new[] { ':' }, 2);
                                    if (parts.Length == 2)
                                    {
                                        specList.Add(new
                                        {
                                            Key = parts[0].Trim(),
                                            Value = parts[1].Trim()
                                        });
                                    }
                                }
                                jsonSpecs = JsonConvert.SerializeObject(specList);
                            }
                            var isFeaturedStr = worksheet.Cells[row, 5].Value?.ToString().Trim().ToUpper();
                            bool isFeatured = (isFeaturedStr == "TRUE" || isFeaturedStr == "1" || isFeaturedStr == "YES");

                            double.TryParse(worksheet.Cells[row, 6].Value?.ToString(), out double discountRate);

                            var color = worksheet.Cells[row, 7].Value?.ToString().Trim();
                            var rom = worksheet.Cells[row, 8].Value?.ToString().Trim();
                            var ram = worksheet.Cells[row, 9].Value?.ToString().Trim();

                            double.TryParse(worksheet.Cells[row, 10].Value?.ToString(), out double price);
                            int.TryParse(worksheet.Cells[row, 11].Value?.ToString(), out int quantity);

                            var avatarUrl = worksheet.Cells[row, 12].Value?.ToString().Trim();
                            var albumUrls = worksheet.Cells[row, 13].Value?.ToString().Trim();

                            int categoryId;
                            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
                            if (category != null) categoryId = category.Id;
                            else
                            {
                                var firstCat = await _context.Categories.FirstOrDefaultAsync();
                                categoryId = firstCat?.Id ?? 1;
                            }

                            var product = await _context.Products.FirstOrDefaultAsync(p => p.Name == productName);
                            bool isNewProduct = false;

                            if (product == null)
                            {
                                isNewProduct = true;
                                product = new Product
                                {
                                    Name = productName,
                                    CategoryId = categoryId,
                                    IsActive = true
                                };
                                _context.Products.Add(product);
                            }

                            product.Description = description ?? "";
                            product.Specifications = jsonSpecs;
                            product.IsFeatured = isFeatured;
                            product.DiscountRate = discountRate;
                            product.ImageUrl = avatarUrl ?? "https://placehold.co/300";

                            await _context.SaveChangesAsync();

                            if (isNewProduct && !string.IsNullOrEmpty(albumUrls))
                            {
                                var urls = albumUrls.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var url in urls)
                                {
                                    _context.ProductImages.Add(new ProductImage
                                    {
                                        ProductId = product.Id,
                                        Url = url.Trim()
                                    });
                                }
                                await _context.SaveChangesAsync();
                            }

                            var existingVariant = await _context.ProductDetails
                                .FirstOrDefaultAsync(d => d.ProductId == product.Id && d.Color == color && d.Rom == rom && d.Ram == ram);

                            if (existingVariant != null)
                            {
                                existingVariant.Quantity += quantity;
                                existingVariant.Price = (decimal)price;
                                _context.ProductDetails.Update(existingVariant);
                            }
                            else
                            {
                                var newVariant = new ProductDetail
                                {
                                    ProductId = product.Id,
                                    Color = color ?? "Mặc định",
                                    Rom = rom ?? "Tiêu chuẩn",
                                    Ram = ram ?? "8GB",
                                    Price = (decimal)price,
                                    Quantity = quantity,
                                    ImageUrl = avatarUrl,
                                    IsActive = true
                                };
                                _context.ProductDetails.Add(newVariant);
                            }
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                TempData["success"] = "Nhập hàng thành công!";
                return RedirectToAction("Index", "Products");
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                if (ex.InnerException != null) msg += " | " + ex.InnerException.Message;
                TempData["error"] = "Lỗi: " + msg;
                return View();
            }
        }
        public IActionResult DownloadTemplate()
        {
            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("NhapHangFull");

                string[] headers = {
                    "Tên Sản Phẩm", "Danh Mục", "Mô Tả Chi Tiết", "Thông Số Kỹ Thuật (Key:Value; Key2:Value2)",
                    "Nổi Bật (TRUE/FALSE)", "Giảm Giá (%)",
                    "Màu Sắc", "ROM", "RAM", "Giá Bán", "Số Lượng",
                    "Ảnh Đại Diện", "Album Ảnh Phụ"
                };

                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                }

                using (var range = worksheet.Cells[1, 1, 1, headers.Length])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightBlue);
                    range.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                }
                worksheet.Cells.AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;
                string excelName = $"Mau_Nhap_Hang_V3_{DateTime.Now:ddMMyyyy}.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excelName);
            }
        }
    }
}