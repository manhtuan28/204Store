using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _204Store.Data;
using _204Store.Models;
using _204Store.Services;
using System.Security.Claims;
using _204Store.Models.Momo;

namespace _204Store.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly VnPayService _vnPayService;
        private readonly MomoService _momoService;

        public CheckoutController(ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            VnPayService vnPayService,
            MomoService momoService)
        {
            _context = context;
            _userManager = userManager;
            _vnPayService = vnPayService;
            _momoService = momoService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _context.CartItems
                .Include(c => c.ProductDetail)
                .ThenInclude(p => p.Product)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Index", "Home");

            string couponCode = HttpContext.Session.GetString("CouponCode");
            if (!string.IsNullOrEmpty(couponCode))
            {
                var promo = await _context.Promotions
                    .FirstOrDefaultAsync(p => p.Code == couponCode && p.IsActive && p.EndDate >= DateTime.Now && p.StartDate <= DateTime.Now);

                if (promo == null)
                {
                    HttpContext.Session.Remove("CouponCode");
                    HttpContext.Session.Remove("DiscountAmount");
                    couponCode = null;
                }
            }

            var user = await _userManager.FindByIdAsync(userId);
            double originalTotal = cartItems.Sum(c => (double)c.ProductDetail.Price * c.Quantity);

            decimal discount = 0;
            string discountStr = HttpContext.Session.GetString("DiscountAmount");
            if (!string.IsNullOrEmpty(discountStr))
            {
                if (decimal.TryParse(discountStr, out decimal parsedDiscount)) discount = parsedDiscount;
            }

            double finalTotal = originalTotal - (double)discount;
            if (finalTotal < 0) finalTotal = 0;

            var orderHeader = new OrderHeader
            {
                Name = user.FullName ?? "",
                PhoneNumber = user.PhoneNumber ?? "",
                OrderTotal = finalTotal,
                StreetAddress = user.Address ?? ""
            };

            ViewBag.CartItems = cartItems;
            ViewBag.OriginalTotal = originalTotal;
            ViewBag.Discount = discount;
            ViewBag.CouponCode = couponCode;

            return View(orderHeader);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(OrderHeader orderHeader, string paymentMethod)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _context.CartItems
                .Include(c => c.ProductDetail)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any()) return RedirectToAction("Index", "Home");

            decimal originalTotal = cartItems.Sum(c => c.ProductDetail.Price * c.Quantity);
            decimal discount = 0;
            string couponCode = HttpContext.Session.GetString("CouponCode");
            string discountStr = HttpContext.Session.GetString("DiscountAmount");

            if (!string.IsNullOrEmpty(couponCode) && !string.IsNullOrEmpty(discountStr))
            {
                discount = decimal.Parse(discountStr);
                orderHeader.CouponCode = couponCode;
                orderHeader.DiscountAmount = discount;
            }

            decimal finalTotal = originalTotal - discount;
            if (finalTotal < 0) finalTotal = 0;

            orderHeader.ApplicationUserId = userId;
            orderHeader.OrderDate = DateTime.Now;
            orderHeader.OrderTotal = (double)finalTotal;

            orderHeader.PaymentStatus = "Pending";
            orderHeader.OrderStatus = "Pending";

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

            HttpContext.Session.Remove("CouponCode");
            HttpContext.Session.Remove("DiscountAmount");

            if (paymentMethod == "VNPAY")
            {
                var url = _vnPayService.CreatePaymentUrl(orderHeader, HttpContext);
                return Redirect(url);
            }
            else if (paymentMethod == "MOMO")
            {
                var response = await _momoService.CreatePaymentAsync(orderHeader);

                if (response != null && response.resultCode == 0 && !string.IsNullOrEmpty(response.payUrl))
                {
                    return Redirect(response.payUrl);
                }
                else
                {
                    string errorMsg = response != null ? response.message : "Không nhận được phản hồi.";
                    TempData["error"] = "Lỗi MoMo: " + errorMsg;
                    return RedirectToAction("OrderConfirmation", new { id = orderHeader.Id });
                }
            }

            return RedirectToAction("OrderConfirmation", new { id = orderHeader.Id });
        }

        public async Task<IActionResult> PaymentCallback()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            if (response == null || response.VnPayResponseCode != "00")
            {
                TempData["error"] = "Thanh toán VNPay thất bại hoặc bị hủy.";
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

        public async Task<IActionResult> MomoCallback()
        {
            if (Request.Query["resultCode"] == "0")
            {
                var orderIdStr = Request.Query["orderId"].ToString();
                try
                {
                    var realOrderIdStr = orderIdStr.Split('_')[0];

                    if (int.TryParse(realOrderIdStr, out int orderId))
                    {
                        var order = await _context.OrderHeaders.FindAsync(orderId);
                        if (order != null)
                        {
                            order.PaymentStatus = "Approved";
                            order.OrderStatus = "Processing";
                            order.TrackingNumber = Request.Query["transId"];
                            await _context.SaveChangesAsync();
                            return RedirectToAction("OrderConfirmation", new { id = orderId });
                        }
                    }
                }
                catch
                {
                }
            }

            TempData["error"] = "Thanh toán MoMo thất bại. Mã lỗi: " + Request.Query["resultCode"];
            return RedirectToAction("Index", "Home");
        }

        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Repay(int orderId, string paymentMethod)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await _context.OrderHeaders.FirstOrDefaultAsync(o => o.Id == orderId && o.ApplicationUserId == userId);

            if (order == null) return NotFound();

            if (order.PaymentStatus == "Approved")
            {
                TempData["error"] = "Đơn hàng này đã được thanh toán rồi.";
                return RedirectToAction("Details", "Order", new { id = orderId });
            }

            if (paymentMethod == "VNPAY")
            {
                var url = _vnPayService.CreatePaymentUrl(order, HttpContext);
                return Redirect(url);
            }
            else if (paymentMethod == "MOMO")
            {
                var response = await _momoService.CreatePaymentAsync(order);
                if (response != null && response.resultCode == 0 && !string.IsNullOrEmpty(response.payUrl))
                {
                    return Redirect(response.payUrl);
                }
                else
                {
                    TempData["error"] = "Lỗi tạo thanh toán MoMo: " + (response?.message ?? "Không xác định");
                    return RedirectToAction("Details", "Order", new { id = orderId });
                }
            }

            TempData["error"] = "Vui lòng chọn phương thức thanh toán hợp lệ.";
            return RedirectToAction("Details", "Order", new { id = orderId });
        }

        [HttpPost]
        public async Task<IActionResult> ApplyCouponAjax(string couponCode)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var promo = await _context.Promotions.FirstOrDefaultAsync(p => p.Code == couponCode && p.IsActive);

            if (promo == null || promo.EndDate < DateTime.Now || promo.StartDate > DateTime.Now)
                return Json(new { success = false, message = "Mã giảm giá không hợp lệ hoặc đã hết hạn!" });

            var cartItems = await _context.CartItems.Include(c => c.ProductDetail).Where(c => c.UserId == userId).ToListAsync();
            decimal originalTotal = cartItems.Sum(c => c.ProductDetail.Price * c.Quantity);

            decimal discount = 0;
            if (promo.DiscountPercent > 0) discount = originalTotal * (promo.DiscountPercent / 100);
            else discount = promo.DiscountAmount;

            HttpContext.Session.SetString("CouponCode", promo.Code);
            HttpContext.Session.SetString("DiscountAmount", discount.ToString());

            decimal finalTotal = originalTotal - discount;
            if (finalTotal < 0) finalTotal = 0;

            return Json(new
            {
                success = true,
                message = $"Áp dụng mã {promo.Code} thành công!",
                discount = discount,
                finalTotal = finalTotal
            });
        }
    }
}