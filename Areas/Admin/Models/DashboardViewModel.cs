using System;
using System.Collections.Generic;

namespace _204Store.Areas.Admin.Models
{
    public class DashboardViewModel
    {
        public double TongDoanhThu { get; set; }
        public int TongDonHang { get; set; }
        public int TongSanPham { get; set; }
        public int TongKhachHang { get; set; }
        public List<OrderViewModel> DonHangMoiNhat { get; set; }
        public List<TopSellingProduct> SanPhamBanChay { get; set; }
    }

    public class OrderViewModel
    {
        public int Id { get; set; }
        public string MaDon { get; set; }
        public string TenKhachHang { get; set; }
        public DateTime NgayDat { get; set; }
        public double TongTien { get; set; }
        public string TrangThai { get; set; }
    }

    public class TopSellingProduct
    {
        public string TenSanPham { get; set; }
        public string HinhAnh { get; set; }
        public double GiaBan { get; set; }
        public int SoLuongBan { get; set; }
    }
}