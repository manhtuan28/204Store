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

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
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
                .FirstOrDefaultAsync(m => m.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
    }
}