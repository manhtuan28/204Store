using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using _204Store.Data;
using _204Store.Areas.Admin.Models;
using _204Store.Models;
using System.Linq;
using System.Threading.Tasks;

namespace _204Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var tongDoanhThu = await _context.OrderHeaders
                    .Where(u => u.PaymentStatus == "Approved" && u.OrderStatus != "Cancelled" && u.OrderStatus != "Refunded")
                    .SumAsync(u => u.OrderTotal);

            var tongDonHang = await _context.OrderHeaders.CountAsync();
            var tongSanPham = await _context.Products.CountAsync();
            var tongKhachHang = await _context.Users.CountAsync();

            var donHangMoi = await _context.OrderHeaders
                .Include(u => u.ApplicationUser)
                .OrderByDescending(u => u.OrderDate)
                .Take(5)
                .Select(u => new OrderViewModel
                {
                    Id = u.Id,
                    MaDon = "#ORD" + u.Id.ToString("D4"),
                    TenKhachHang = u.ApplicationUser != null ? u.ApplicationUser.FullName : u.Name,
                    NgayDat = u.OrderDate,
                    TongTien = u.OrderTotal,
                    TrangThai = u.OrderStatus
                })
                .ToListAsync();

            var topSanPhamQuery = await _context.OrderDetails
                .Include(d => d.ProductDetail)
                .ThenInclude(pd => pd.Product)
                .GroupBy(d => new {
                    d.ProductDetail.ProductId,
                    d.ProductDetail.Product.Name,
                    d.ProductDetail.Product.ImageUrl
                })
                .Select(g => new TopSellingProduct
                {
                    TenSanPham = g.Key.Name,
                    HinhAnh = g.Key.ImageUrl,
                    GiaBan = g.Max(x => x.Price),
                    SoLuongBan = g.Sum(x => x.Count)
                })
                .OrderByDescending(x => x.SoLuongBan)
                .Take(5)
                .ToListAsync();


            var model = new DashboardViewModel
            {
                TongDoanhThu = tongDoanhThu,
                TongDonHang = tongDonHang,
                TongSanPham = tongSanPham,
                TongKhachHang = tongKhachHang,
                DonHangMoiNhat = donHangMoi,
                SanPhamBanChay = topSanPhamQuery
            };

            return View(model);
        }
    }
}