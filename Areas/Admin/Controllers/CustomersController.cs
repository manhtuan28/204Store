using _204Store.Data;
using _204Store.Models;
using _204Store.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _204Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Manager")]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
            var adminUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == adminRole.Id)
                .Select(ur => ur.UserId)
                .ToListAsync();

            var customers = await _context.Users
                .Where(u => !adminUserIds.Contains(u.Id))
                .ToListAsync();

            return View(customers);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var orders = await _context.OrderHeaders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductDetail)
                .ThenInclude(pd => pd.Product)
                .Where(o => o.ApplicationUserId == id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var logins = await _context.LoginHistories
                .Where(l => l.ApplicationUserId == id)
                .OrderByDescending(l => l.LoginTime)
                .Take(50)
                .ToListAsync();

            var viewModel = new CustomerDetailVM
            {
                UserInfo = user,
                OrderHistory = orders,
                ReturnHistory = orders.Where(o => o.OrderStatus == "Refunded" || o.OrderStatus == "Returned").ToList(),
                LoginLogs = logins,
                TotalOrders = orders.Count,
                TotalSpent = (decimal)orders.Sum(o => o.OrderTotal)
            };

            return View(viewModel);
        }
    }
}