using _204Store.Data;
using _204Store.Models;
using _204Store.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;

namespace _204Store.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

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

        public async Task<IActionResult> Index(int? category, string? query)
        {
            var productsQuery = _context.Products
                .Include(p => p.Category)
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

                    var categoryIds = new List<int> { category.Value };
                    var childIds = await _context.Categories
                        .Where(c => c.ParentId == category.Value)
                        .Select(c => c.Id)
                        .ToListAsync();

                    categoryIds.AddRange(childIds);

                    productsQuery = productsQuery.Where(p => categoryIds.Contains(p.CategoryId));
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

                viewModel.FeaturedProducts = allProducts.Where(p => p.IsFeatured).Take(8).ToList();
                viewModel.BestSellerProducts = allProducts.OrderByDescending(p => p.Sold).Take(8).ToList();
                viewModel.SaleProducts = allProducts.Where(p => p.DiscountRate > 0).OrderByDescending(p => p.DiscountRate).Take(8).ToList();
                viewModel.AppleProducts = allProducts.Where(p => p.Category.Name.Contains("Apple") || p.Category.Name.Contains("iPhone")).OrderByDescending(p => p.Id).Take(4).ToList(); // Tăng lên 4 cho đẹp
                viewModel.SamsungProducts = allProducts.Where(p => p.Category.Name.Contains("Samsung") || p.Category.Name.Contains("Galaxy")).OrderByDescending(p => p.Id).Take(4).ToList();
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

        public IActionResult StoreLocator(string? city)
        {
            var allStores = new List<StoreLocation>
    {
        new StoreLocation { Id=1, City="Hà Nội", Name="204Store Đại Cồ Việt", Address="Số 1 Đại Cồ Việt, Q. Hai Bà Trưng", Phone="0988.111.111", MapUrl="https://goo.gl/maps/example" },
        new StoreLocation { Id=2, City="Hà Nội", Name="204Store Thái Hà", Address="120 Thái Hà, Q. Đống Đa", Phone="0988.222.222", MapUrl="#" },
        new StoreLocation { Id=3, City="Hà Nội", Name="204Store Cầu Giấy", Address="255 Cầu Giấy, Q. Cầu Giấy", Phone="0988.333.333", MapUrl="#" },
        new StoreLocation { Id=4, City="Hà Nội", Name="204Store Hà Đông", Address="10 Trần Phú, Q. Hà Đông", Phone="0988.444.444", MapUrl="#" },
        new StoreLocation { Id=5, City="Hà Nội", Name="204Store Aeon Long Biên", Address="Tầng 1 Aeon Mall Long Biên", Phone="0988.555.555", MapUrl="#" },
        new StoreLocation { Id=6, City="Hà Nội", Name="204Store Xã Đàn", Address="450 Xã Đàn, Q. Đống Đa", Phone="0988.666.666", MapUrl="#" },

        new StoreLocation { Id=7, City="TP.HCM", Name="204Store Quận 1", Address="123 Nguyễn Huệ, Quận 1", Phone="0909.111.111", MapUrl="#" },
        new StoreLocation { Id=8, City="TP.HCM", Name="204Store Quận 10", Address="456 Đường 3/2, Quận 10", Phone="0909.222.222", MapUrl="#" },
        new StoreLocation { Id=9, City="TP.HCM", Name="204Store Gò Vấp", Address="789 Quang Trung, Gò Vấp", Phone="0909.333.333", MapUrl="#" },
        new StoreLocation { Id=10, City="TP.HCM", Name="204Store Bình Thạnh", Address="200 Xô Viết Nghệ Tĩnh, Bình Thạnh", Phone="0909.444.444", MapUrl="#" },
        new StoreLocation { Id=11, City="TP.HCM", Name="204Store Thủ Đức", Address="15 Võ Văn Ngân, TP. Thủ Đức", Phone="0909.555.555", MapUrl="#" },
        new StoreLocation { Id=12, City="TP.HCM", Name="204Store Quận 7", Address="101 Nguyễn Thị Thập, Quận 7", Phone="0909.666.666", MapUrl="#" },
        new StoreLocation { Id=13, City="TP.HCM", Name="204Store Tân Bình", Address="300 Cộng Hòa, Tân Bình", Phone="0909.777.777", MapUrl="#" },
        new StoreLocation { Id=14, City="TP.HCM", Name="204Store Quận 5", Address="150 Nguyễn Trãi, Quận 5", Phone="0909.888.888", MapUrl="#" },

        new StoreLocation { Id=15, City="Đà Nẵng", Name="204Store Nguyễn Văn Linh", Address="123 Nguyễn Văn Linh, Q. Hải Châu", Phone="0911.111.111", MapUrl="#" },
        new StoreLocation { Id=16, City="Đà Nẵng", Name="204Store Lê Duẩn", Address="45 Lê Duẩn, Q. Hải Châu", Phone="0911.222.222", MapUrl="#" },
        new StoreLocation { Id=17, City="Hải Phòng", Name="204Store Lạch Tray", Address="100 Lạch Tray, Q. Ngô Quyền", Phone="0922.111.111", MapUrl="#" },
        new StoreLocation { Id=18, City="Cần Thơ", Name="204Store Ninh Kiều", Address="50 Đường 30/4, Q. Ninh Kiều", Phone="0933.111.111", MapUrl="#" },
        new StoreLocation { Id=19, City="Nha Trang", Name="204Store Nha Trang", Address="20 Thái Nguyên, TP. Nha Trang", Phone="0944.111.111", MapUrl="#" },
        new StoreLocation { Id=20, City="Bình Dương", Name="204Store Thủ Dầu Một", Address="500 Đại Lộ Bình Dương", Phone="0955.111.111", MapUrl="#" },
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