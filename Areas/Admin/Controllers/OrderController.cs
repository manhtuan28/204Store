using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _204Store.Data;
using _204Store.Models;

namespace _204Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string status = "all")
        {
            var query = _context.OrderHeaders.AsQueryable();

            if (status != "all")
            {
                query = query.Where(o => o.OrderStatus == status);
            }

            var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var orderHeader = await _context.OrderHeaders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductDetail)
                .ThenInclude(p => p.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (orderHeader == null) return NotFound();

            return View(orderHeader);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.OrderHeaders.FindAsync(id);
            if (order == null) return NotFound();

            order.OrderStatus = status;

            if (status == "Completed" && order.PaymentStatus == "Pending")
            {
                order.PaymentStatus = "Approved";
            }
            if (status == "Cancelled")
            {
            }

            await _context.SaveChangesAsync();
            TempData["success"] = "Cập nhật trạng thái đơn hàng thành công!";

            return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.OrderHeaders.FindAsync(id);
            if (order != null)
            {
                _context.OrderHeaders.Remove(order);
                await _context.SaveChangesAsync();
                TempData["success"] = "Đã xóa đơn hàng vĩnh viễn.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessReturn(int id, string action)
        {
            var order = await _context.OrderHeaders.FindAsync(id);
            if (order == null) return NotFound();

            if (action == "approve")
            {
                order.OrderStatus = "Refunded";
                order.PaymentStatus = "Refunded";

                TempData["success"] = "Đã chấp thuận trả hàng & Hoàn tiền.";
            }
            else
            {
                order.OrderStatus = "Completed";
                TempData["success"] = "Đã từ chối yêu cầu trả hàng.";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}