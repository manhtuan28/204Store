using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _204Store.Data;
using _204Store.Models;
using _204Store.Services;
using System.Security.Claims;

namespace _204Store.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly VnPayService _vnPayService;

        public CheckoutController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, VnPayService vnPayService)
        {
            _context = context;
            _userManager = userManager;
            _vnPayService = vnPayService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _context.CartItems.Include(c => c.ProductDetail).ThenInclude(p => p.Product).Where(c => c.UserId == userId).ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);
            var orderHeader = new OrderHeader
            {
                Name = user.FullName ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                OrderTotal = (double)cartItems.Sum(c => c.ProductDetail.Price * c.Quantity)
            };

            ViewBag.CartItems = cartItems;
            return View(orderHeader);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(OrderHeader orderHeader, string paymentMethod)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _context.CartItems.Include(c => c.ProductDetail).Where(c => c.UserId == userId).ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Index", "Home");

            orderHeader.ApplicationUserId = userId;
            orderHeader.OrderDate = DateTime.Now;
            orderHeader.OrderTotal = (double)cartItems.Sum(c => c.ProductDetail.Price * c.Quantity);
            if (paymentMethod == "COD")
            {
                orderHeader.PaymentStatus = "Pending";
                orderHeader.OrderStatus = "Pending";
            }
            else if (paymentMethod == "VNPAY")
            {
                orderHeader.PaymentStatus = "Pending";
                orderHeader.OrderStatus = "Pending";
            }

            _context.OrderHeaders.Add(orderHeader);
            await _context.SaveChangesAsync();

            foreach (var item in cartItems)
            {
                _context.OrderDetails.Add(new OrderDetail
                {
                    OrderHeaderId = orderHeader.Id,
                    ProductDetailId = item.ProductDetailId,
                    Price = (double)item.ProductDetail.Price,
                    Count = item.Quantity
                });
            }

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            if (paymentMethod == "VNPAY")
            {
                var url = _vnPayService.CreatePaymentUrl(orderHeader, HttpContext);
                return Redirect(url);
            }
            else if (paymentMethod == "MOMO")
            {
                return RedirectToAction("OrderConfirmation", new { id = orderHeader.Id });
            }

            return RedirectToAction("OrderConfirmation", new { id = orderHeader.Id });
        }

        public async Task<IActionResult> PaymentCallback()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            if (response == null || response.VnPayResponseCode != "00")
            {
                TempData["error"] = "Thanh toán thất bại hoặc bị hủy.";
                return RedirectToAction("Index", "Home");
            }

            var orderId = int.Parse(response.OrderId);
            var order = await _context.OrderHeaders.FindAsync(orderId);
            if (order != null)
            {
                order.PaymentStatus = "Approved";
                order.OrderStatus = "Processing";
                order.TrackingNumber = response.TransactionId;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("OrderConfirmation", new { id = orderId });
        }

        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
        }

        [Authorize]
        public async Task<IActionResult> Repay(int orderId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var order = await _context.OrderHeaders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.ApplicationUserId == userId);

            if (order == null) return NotFound();

            if (order.PaymentStatus == "Approved")
            {
                TempData["error"] = "Đơn hàng này đã được thanh toán rồi.";
                return RedirectToAction("Details", "Order", new { id = orderId });
            }

            if (order.OrderStatus == "Cancelled")
            {
                TempData["error"] = "Đơn hàng đã bị hủy, không thể thanh toán.";
                return RedirectToAction("Details", "Order", new { id = orderId });
            }

            var url = _vnPayService.CreatePaymentUrl(order, HttpContext);

            return Redirect(url);
        }
    }
}