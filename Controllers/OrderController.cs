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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestReturn(int id, string reason)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.OrderHeaders.FirstOrDefaultAsync(o => o.Id == id && o.ApplicationUserId == userId);

            if (order == null) return NotFound();

            if (order.ReturnAttempts >= 2)
            {
                TempData["error"] = "Bạn đã hết lượt yêu cầu trả hàng cho đơn này (Tối đa 2 lần).";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            if (order.OrderStatus == "Completed")
            {
                order.OrderStatus = "ReturnRequested";
                order.ReturnReason = reason;

                order.ReturnAttempts += 1;

                await _context.SaveChangesAsync();
                TempData["success"] = "Đã gửi yêu cầu trả hàng. (Lần thứ " + order.ReturnAttempts + "/2)";
            }
            else
            {
                TempData["error"] = "Đơn hàng không đủ điều kiện.";
            }

            return RedirectToAction("Index", "Order", new { id = id });
        }
    }
}