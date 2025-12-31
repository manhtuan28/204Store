using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _204Store.Data;
using _204Store.Models;
using _204Store.Models.ViewModels;
using System.Diagnostics;

namespace _204Store.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;



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
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
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

        public async Task<IActionResult> Index()
        {
            var allProducts = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductDetails)
                .AsQueryable();

            var viewModel = new HomeViewModel
            {
                // Danh mục
                Categories = await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync(),

                // Sản phẩm Nổi bật
                FeaturedProducts = await allProducts
                    .Where(p => p.IsFeatured)
                    .Take(8)
                    .ToListAsync(),

                // Sản phẩm Bán chạy
                BestSellerProducts = await allProducts
                    .OrderByDescending(p => p.Sold)
                    .Take(8)
                    .ToListAsync(),

                // Sản phẩm Giảm giá
                SaleProducts = await allProducts
                    .Where(p => p.DiscountRate > 0)
                    .OrderByDescending(p => p.DiscountRate)
                    .Take(8)
                    .ToListAsync(),

                // Apple
                AppleProducts = await allProducts
                    .Where(p => p.Category.Name.Contains("Apple") || p.Category.Name.Contains("iPhone"))
                    .OrderByDescending(p => p.Id)
                    .Take(2)
                    .ToListAsync(),

                // Samsung
                SamsungProducts = await allProducts
                    .Where(p => p.Category.Name.Contains("Samsung") || p.Category.Name.Contains("Galaxy"))
                    .OrderByDescending(p => p.Id)
                    .Take(2)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
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
                .Include(p => p.ProductDetails)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            var relatedProducts = await _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.Id != productId)
                .Include(p => p.ProductDetails)
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
    }
}