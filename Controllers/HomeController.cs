using _204Store.Data;
using _204Store.Models;
using _204Store.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace _204Store.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;

        public IActionResult Terms()
        {
            return View();
        }
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult Careers()
        {
            return View();
        }
        public IActionResult Contact()
        {
            return View();
        }
        public IActionResult Guide()
        {
            return View();
        }
        public IActionResult Warranty()
        {
            return View();
        }
        public IActionResult ReturnPolicy()
        {
            return View();
        }
        public IActionResult ServiceCenter()
        {
            return View();
        }
        public IActionResult Partnership()
        {
            return View();
        }
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IEmailSender emailSender)
        {
            _logger = logger;
            _context = context;
            _emailSender = emailSender;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendFeedback(string fullName, string email, string type, string content)
        {

            if (type == "Khiếu nại")
            {
                TempData["success"] = "Đã tiếp nhận khiếu nại. Quản lý sẽ liên hệ bạn trong vòng 4 giờ làm việc.";
            }
            else
            {
                TempData["success"] = "Cảm ơn những đóng góp quý báu của bạn dành cho 204Store!";
            }

            return RedirectToAction(nameof(Contact));
        }

        public async Task<IActionResult> Index(int? category, string? query)
        {
            var productsQuery = _context.Products
                //.Where(p => p.IsActive)
                .Include(p => p.Category)
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .Include(p => p.ProductDetails)
                .AsQueryable();

            bool isFiltering = false;
            string pageTitle = "Trang chủ";

            if (category.HasValue)
            {
                isFiltering = true;
                var selectedCat = await _context.Categories.FindAsync(category.Value);

                if (selectedCat != null)
                {
                    pageTitle = selectedCat.Name;

                    // Lấy danh sách ID gồm category hiện tại + các danh mục con
                    var categoryIds = new List<int> { category.Value };
                    var childIds = await _context.Categories
                        .Where(c => c.ParentId == category.Value)
                        .Select(c => c.Id)
                        .ToListAsync();

                    categoryIds.AddRange(childIds);

                    // Lọc theo ProductCategories (nhiều-nhiều) HOẶC CategoryId (tương thích ngược)
                    productsQuery = productsQuery.Where(p => 
                        p.ProductCategories.Any(pc => categoryIds.Contains(pc.CategoryId)) ||
                        categoryIds.Contains(p.CategoryId));
                }
            }

            if (!string.IsNullOrEmpty(query))
            {
                isFiltering = true;
                pageTitle = $"Kết quả tìm kiếm: '{query}'";
                productsQuery = productsQuery.Where(p => p.Name.Contains(query));
            }

            var viewModel = new HomeViewModel();

            viewModel.Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();

            if (isFiltering)
            {
                viewModel.FeaturedProducts = await productsQuery.OrderByDescending(p => p.Id).ToListAsync();

                viewModel.BestSellerProducts = new List<Product>();
                viewModel.SaleProducts = new List<Product>();
                viewModel.AppleProducts = new List<Product>();
                viewModel.SamsungProducts = new List<Product>();
            }
            else
            {
                var allProducts = await productsQuery.ToListAsync();

                viewModel.FeaturedProducts = allProducts.Where(p => p.IsFeatured).Take(10).ToList();
                viewModel.BestSellerProducts = allProducts.OrderByDescending(p => p.Sold).Take(8).ToList();
                viewModel.SaleProducts = allProducts.Where(p => p.DiscountRate > 0).OrderByDescending(p => p.DiscountRate).Take(8).ToList();

                // Lọc theo ProductCategories hoặc Category
                viewModel.AppleProducts = allProducts.Where(p => 
                    p.ProductCategories.Any(pc => pc.Category?.Name?.Contains("Apple") == true || pc.Category?.Name?.Contains("iPhone") == true) ||
                    p.Category?.Name?.Contains("Apple") == true || 
                    p.Category?.Name?.Contains("iPhone") == true
                ).OrderByDescending(p => p.Id).Take(4).ToList();

                viewModel.SamsungProducts = allProducts.Where(p => 
                    p.ProductCategories.Any(pc => pc.Category?.Name?.Contains("Samsung") == true || pc.Category?.Name?.Contains("Galaxy") == true) ||
                    p.Category?.Name?.Contains("Samsung") == true || 
                    p.Category?.Name?.Contains("Galaxy") == true
                ).OrderByDescending(p => p.Id).Take(4).ToList();

                viewModel.LaptopProducts = allProducts
                    .Where(p => 
                        p.ProductCategories.Any(pc => pc.Category?.Name?.Contains("Laptop") == true || pc.Category?.Name?.Contains("MacBook") == true) ||
                        p.Category?.Name?.Contains("Laptop") == true || 
                        p.Category?.Name?.Contains("MacBook") == true)
                    .OrderByDescending(p => p.Id)
                    .Take(8)
                    .ToList();

                viewModel.AccessoryProducts = allProducts
                    .Where(p => 
                        p.ProductCategories.Any(pc => 
                            pc.Category?.Name?.Contains("Phụ kiện") == true || 
                            pc.Category?.Name?.Contains("Tai nghe") == true || 
                            pc.Category?.Name?.Contains("Sạc") == true) ||
                        p.Category?.Name?.Contains("Phụ kiện") == true || 
                        p.Category?.Name?.Contains("Tai nghe") == true || 
                        p.Category?.Name?.Contains("Sạc") == true)
                    .OrderByDescending(p => p.Id)
                    .Take(8)
                    .ToList();
            }

            ViewData["IsFiltering"] = isFiltering;
            ViewData["PageTitle"] = pageTitle;
            ViewBag.SearchQuery = query;

            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult PageNotFound()
        {
            Response.StatusCode = 404;
            return View();
        }

        public async Task<IActionResult> Details(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductCategories)
                    .ThenInclude(pc => pc.Category)
                .Include(p => p.ProductDetails)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            // Lấy sản phẩm liên quan dựa trên ProductCategories hoặc CategoryId
            var productCategoryIds = product.ProductCategories?.Select(pc => pc.CategoryId).ToList() ?? new List<int>();
            if (product.CategoryId > 0 && !productCategoryIds.Contains(product.CategoryId))
            {
                productCategoryIds.Add(product.CategoryId);
            }

            var relatedProducts = await _context.Products
                .Include(p => p.ProductDetails)
                .Include(p => p.ProductCategories)
                .Where(p => p.Id != productId && 
                    (p.ProductCategories.Any(pc => productCategoryIds.Contains(pc.CategoryId)) ||
                     productCategoryIds.Contains(p.CategoryId)))
                .Take(4)
                .ToListAsync();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        [HttpGet]
        public IActionResult Tracking(int? orderId, string? phone)
        {
            if (orderId.HasValue && !string.IsNullOrEmpty(phone))
            {
                return TrackingOrder(orderId.Value, phone).Result;
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Subscribe(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { success = false, message = "Vui lòng nhập email." });
            }

            var exists = await _context.Subscribers.AnyAsync(s => s.Email == email);
            if (exists)
            {
                return Json(new { success = false, message = "Email này đã đăng ký nhận tin rồi!" });
            }

            var sub = new Subscriber { Email = email };
            _context.Subscribers.Add(sub);
            await _context.SaveChangesAsync();

            string baseUrl = $"{Request.Scheme}://{Request.Host}";

            string voucherCode = "CHAO206";
            string subject = "🎁 Quà làm quen: Tặng bạn mã giảm giá 20%";

            string htmlMessage = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden;'>
                <div style='background-color: #0d6efd; padding: 20px; text-align: center; color: white;'>
                    <h2 style='margin: 0;'>Chào mừng bạn đến với 204Store!</h2>
                </div>
                <div style='padding: 30px; background-color: #ffffff;'>
                    <p>Chào bạn,</p>
                    <p>Cảm ơn bạn đã đăng ký nhận bản tin. Chúng tôi rất vui được đồng hành cùng bạn.</p>
                    <p>Như một món quà làm quen, 204Store gửi tặng bạn mã giảm giá cho đơn hàng đầu tiên:</p>
                    
                    <div style='background-color: #f8f9fa; border: 2px dashed #0d6efd; padding: 15px; text-align: center; margin: 20px 0; font-size: 24px; font-weight: bold; color: #0d6efd; letter-spacing: 2px;'>
                        {voucherCode}
                    </div>

                    <p>Hãy sử dụng mã này tại bước thanh toán để được giảm ngay <strong>20%</strong> (hoặc 150.000đ).</p>
                    
                    <div style='text-align: center; margin-top: 30px;'>
                        <a href='{baseUrl}' style='background-color: #0d6efd; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Mua sắm ngay</a>
                    </div>
                </div>
                <div style='background-color: #f1f1f1; padding: 15px; text-align: center; font-size: 12px; color: #666;'>
                    <p>© 2026 204Store. All rights reserved.</p>
                    <p>Truy cập website: <a href='{baseUrl}'>{baseUrl}</a></p>
                </div>
            </div>
        ";

            try
            {
                await _emailSender.SendEmailAsync(email, subject, htmlMessage);
                return Json(new { success = true, message = "Đăng ký thành công! Hãy kiểm tra email để nhận quà nhé." });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Lỗi gửi email, vui lòng thử lại sau." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> TrackingOrder(int orderId, string phone)
        {
            if (orderId <= 0 || string.IsNullOrEmpty(phone))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ Mã đơn hàng và Số điện thoại.";
                return View("Tracking");
            }

            var order = await _context.OrderHeaders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductDetail)
                .ThenInclude(p => p.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.PhoneNumber == phone);

            if (order == null)
            {
                ViewBag.Error = "Không tìm thấy đơn hàng hoặc số điện thoại không khớp.";
                return View("Tracking");
            }

            return View("Tracking", order);
        }

        [HttpGet]
        public async Task<IActionResult> SearchLive(string query)
        {
            if (string.IsNullOrEmpty(query)) return Json(new List<object>());

            query = query.Trim();

            var products = await _context.Products
                .Include(p => p.ProductDetails)
                .Where(p => EF.Functions.Like(p.Name, $"%{query}%"))
                .OrderByDescending(p => p.Id)
                .Take(5)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    image = p.ImageUrl,
                    price = p.ProductDetails.Any() ? p.ProductDetails.Min(d => d.Price) : 0
                })
                .ToListAsync();

            return Json(products);
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            ViewBag.Keyword = query;
            if (string.IsNullOrEmpty(query)) return View(new List<Product>());

            query = query.Trim();

            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductDetails)
                .Where(p => EF.Functions.Like(p.Name, $"%{query}%"))
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View(products);
        }

        public IActionResult StoreLocator(string? city)
        {
            var allStores = new List<StoreLocation>
        {
            // VIỆT TRÌ
            new StoreLocation { Id=1, City="Việt Trì", Name="204Store Hùng Vương",       Address="Số 1 Đường Hùng Vương, P. Tiên Cát, TP. Việt Trì, Phú Thọ", Phone="0988.111.111", MapUrl="https://www.google.com/maps?q=Số+1+Đường+Hùng+Vương,+Tiên+Cát,+Việt+Trì,+Phú+Thọ" },
            new StoreLocation { Id=2, City="Việt Trì", Name="204Store Trần Phú",         Address="120 Trần Phú, P. Gia Cẩm, TP. Việt Trì, Phú Thọ",            Phone="0988.222.222", MapUrl="https://www.google.com/maps?q=120+Trần+Phú,+Gia+Cẩm,+Việt+Trì,+Phú+Thọ" },
            new StoreLocation { Id=3, City="Việt Trì", Name="204Store Nguyễn Tất Thành", Address="255 Nguyễn Tất Thành, P. Nông Trang, TP. Việt Trì, Phú Thọ", Phone="0988.333.333", MapUrl="https://www.google.com/maps?q=255+Nguyễn+Tất+Thành,+Nông+Trang,+Việt+Trì,+Phú+Thọ" },
            new StoreLocation { Id=4, City="Việt Trì", Name="204Store Vincom Việt Trì",  Address="Vincom Plaza Việt Trì, P. Vân Phú, TP. Việt Trì, Phú Thọ",   Phone="0988.444.444", MapUrl="https://www.google.com/maps?q=Vincom+Plaza+Việt+Trì,+Vân+Phú,+Việt+Trì,+Phú+Thọ" },

            // HÀ NỘI
            new StoreLocation { Id=5, City="Hà Nội", Name="204Store Đại Cồ Việt", Address="Số 1 Đại Cồ Việt, Q. Hai Bà Trưng", Phone="0988.555.555", MapUrl="https://www.google.com/maps?q=Số+1+Đại+Cồ+Việt,+Hai+Bà+Trưng,+Hà+Nội" },
            new StoreLocation { Id=6, City="Hà Nội", Name="204Store Thái Hà",     Address="120 Thái Hà, Q. Đống Đa",           Phone="0988.666.666", MapUrl="https://www.google.com/maps?q=120+Thái+Hà,+Đống+Đa,+Hà+Nội" },
            new StoreLocation { Id=7, City="Hà Nội", Name="204Store Cầu Giấy",    Address="255 Cầu Giấy, Q. Cầu Giấy",        Phone="0988.777.777", MapUrl="https://www.google.com/maps?q=255+Cầu+Giấy,+Cầu+Giấy,+Hà+Nội" },
            new StoreLocation { Id=8, City="Hà Nội", Name="204Store Hà Đông",     Address="10 Trần Phú, Q. Hà Đông",          Phone="0988.888.888", MapUrl="https://www.google.com/maps?q=10+Trần+Phú,+Hà+Đông,+Hà+Nội" },

            // HỒ CHÍ MINH
            new StoreLocation { Id=9,  City="Hồ Chí Minh", Name="204Store Quận 1",      Address="123 Nguyễn Huệ, Quận 1",       Phone="0909.111.111", MapUrl="https://www.google.com/maps?q=123+Nguyễn+Huệ,+Quận+1,+TP.+Hồ+Chí+Minh" },
            new StoreLocation { Id=10, City="Hồ Chí Minh", Name="204Store Quận 10",     Address="456 Đường 3/2, Quận 10",       Phone="0909.222.222", MapUrl="https://www.google.com/maps?q=456+Đường+3/2,+Quận+10,+TP.+Hồ+Chí+Minh" },
            new StoreLocation { Id=11, City="Hồ Chí Minh", Name="204Store Gò Vấp",      Address="789 Quang Trung, Gò Vấp",      Phone="0909.333.333", MapUrl="https://www.google.com/maps?q=789+Quang+Trung,+Gò+Vấp,+TP.+Hồ+Chí+Minh" },
            new StoreLocation { Id=12, City="Hồ Chí Minh", Name="204Store Thủ Đức",     Address="15 Võ Văn Ngân, TP. Thủ Đức",  Phone="0909.444.444", MapUrl="https://www.google.com/maps?q=15+Võ+Văn+Ngân,+Thủ+Đức,+TP.+Hồ+Chí+Minh" },

            // ĐÀ NẴNG
            new StoreLocation { Id=13, City="Đà Nẵng", Name="204Store Nguyễn Văn Linh", Address="123 Nguyễn Văn Linh, Q. Hải Châu", Phone="0911.111.111", MapUrl="https://www.google.com/maps?q=123+Nguyễn+Văn+Linh,+Hải+Châu,+Đà+Nẵng" },
            new StoreLocation { Id=14, City="Đà Nẵng", Name="204Store Lê Duẩn",         Address="45 Lê Duẩn, Q. Hải Châu",          Phone="0911.222.222", MapUrl="https://www.google.com/maps?q=45+Lê+Duẩn,+Hải+Châu,+Đà+Nẵng" },

            // HẢI PHÒNG
            new StoreLocation { Id=15, City="Hải Phòng", Name="204Store Lạch Tray",     Address="100 Lạch Tray, Q. Ngô Quyền", Phone="0922.111.111", MapUrl="https://www.google.com/maps?q=100+Lạch+Tray,+Ngô+Quyền,+Hải+Phòng" },
            new StoreLocation { Id=16, City="Hải Phòng", Name="204Store Cầu Đất",       Address="50 Cầu Đất, Q. Ngô Quyền",     Phone="0922.222.222", MapUrl="https://www.google.com/maps?q=50+Cầu+Đất,+Ngô+Quyền,+Hải+Phòng" },
        };

            if (!string.IsNullOrEmpty(city) && city != "All")
            {
                allStores = allStores.Where(s => s.City == city).ToList();
            }

            ViewBag.CurrentCity = city;

            return View(allStores);
        }
    }
}

public class StoreLocation
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string Phone { get; set; }
    public string MapUrl { get; set; }
}