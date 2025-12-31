using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _204Store.Data;
using _204Store.Models;
using System.Security.Claims;

namespace _204Store.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orders = await _context.OrderHeaders
                .Where(o => o.ApplicationUserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> Details(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orderHeader = await _context.OrderHeaders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductDetail)
                .ThenInclude(p => p.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.ApplicationUserId == userId);

            if (orderHeader == null) return NotFound();

            return View(orderHeader);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.OrderHeaders.FirstOrDefaultAsync(o => o.Id == id && o.ApplicationUserId == userId);

            if (order != null && order.OrderStatus == "Pending")
            {
                order.OrderStatus = "Cancelled";
                order.PaymentStatus = "Refunded/Cancelled";
                await _context.SaveChangesAsync();
                TempData["success"] = "Đã hủy đơn hàng thành công.";
            }
            else
            {
                TempData["error"] = "Không thể hủy đơn hàng này.";
            }

            return RedirectToAction(nameof(Details), new { orderId = id });
        }
    }
}