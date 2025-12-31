# 📱 204Store - Tech E-commerce Platform

![.NET Core](https://img.shields.io/badge/.NET%20Core-8.0-purple?style=for-the-badge&logo=dotnet)
![EF Core](https://img.shields.io/badge/Entity%20Framework-Core-blue?style=for-the-badge&logo=nuget)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-red?style=for-the-badge&logo=microsoft-sql-server)
![Bootstrap](https://img.shields.io/badge/Bootstrap-5-xyz?style=for-the-badge&logo=bootstrap)
![License](https://img.shields.io/badge/License-MIT-green?style=for-the-badge)

> **204Store** là dự án website thương mại điện tử chuyên kinh doanh các thiết bị công nghệ (Điện thoại, Laptop, Phụ kiện). Hệ thống được xây dựng theo kiến trúc **Monolithic** chuẩn mực trên nền tảng .NET 8, tập trung vào hiệu năng, bảo mật và khả năng mở rộng.

---

## 🌟 Tính năng nổi bật (Key Features)

### 🛍️ Phân hệ Khách hàng (Storefront)
- [x] **Trang chủ:** Hiển thị sản phẩm nổi bật, banner khuyến mãi.
- [x] **Danh mục:** Lọc sản phẩm theo hãng (Apple, Samsung, Xiaomi...).
- [x] **Chi tiết sản phẩm:** Xem biến thể (Màu sắc, RAM/ROM), giá bán động theo phiên bản.
- [x] **Giỏ hàng & Thanh toán:** Thêm vào giỏ, đặt hàng (Checkout) - *In Progress*.
- [x] **Tài khoản:** Xem lịch sử đơn hàng, cập nhật thông tin cá nhân.

### 🛡️ Phân hệ Quản trị (Admin Dashboard)
- [x] **Authentication:** Đăng ký/Đăng nhập bảo mật với ASP.NET Core Identity.
- [x] **Authorization:** Phân quyền chặt chẽ (Admin - Quản trị, Customer - Khách hàng).
- [x] **Product Management:** CRUD sản phẩm, quản lý kho hàng (Inventory) theo từng SKU.
- [x] **Database Seeding:** Tự động tạo dữ liệu mẫu và tài khoản Admin khi chạy lần đầu.

---

## 🛠 Công nghệ sử dụng (Tech Stack)

| Lĩnh vực | Công nghệ | Chi tiết |
| :--- | :--- | :--- |
| **Backend** | ASP.NET Core MVC | .NET 8.0 (Latest Long-term Support) |
| **Database** | SQL Server | Entity Framework Core (Code-First) |
| **Frontend** | Razor Views | Bootstrap 5, jQuery, FontAwesome |
| **Security** | ASP.NET Identity | Authentication, Authorization, Role Management |
| **Tools** | Visual Studio 2022 | SSMS, Git |

---

## 🚀 Hướng dẫn cài đặt & Chạy (Getting Started)

Dành cho Developer clone dự án về máy local.

### 1. Yêu cầu hệ thống (Prerequisites)
* [.NET SDK 8.0](https://dotnet.microsoft.com/download)
* [SQL Server](https://www.microsoft.com/sql-server/) (Bản Express hoặc Full)
* Visual Studio 2022 (Workload: *ASP.NET and web development*)

### 2. Các bước cài đặt (Installation)

**Bước 1: Clone dự án**
```bash
git clone https://github.com/manhtuan28/204Store.git
cd 204Store
```

**Bước 2: Cấu hình Database**
Mở file `appsettings.json`, cập nhật `ConnectionStrings` phù hợp với tên Server máy bạn:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=204Store;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```
*(Lưu ý: Nếu dùng SQL Express thì Server thường là `.\\SQLEXPRESS` hoặc `(localdb)\\mssqllocaldb`)*

**Bước 3: Khởi tạo Database (Migration)**
Mở **Visual Studio** -> **Tools** -> **NuGet Package Manager** -> **Package Manager Console** và chạy lệnh:
```powershell
Update-Database
```
*Lệnh này sẽ tự động sinh ra database `204Store`, tạo bảng và nạp dữ liệu mẫu.*

**Bước 4: Chạy dự án**
Nhấn `F5` hoặc `Ctrl + F5` để khởi chạy website.

---

## 🔐 Tài khoản Demo (Seeding Data)

Sau khi chạy project lần đầu, hệ thống tự động tạo sẵn các tài khoản sau để test:

| Vai trò (Role) | Email | Password | Ghi chú |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin@gmail.com` | `Admin@123` | Full quyền quản trị |
| **Customer** | *(Đăng ký mới)* | *(Tự chọn)* | Mua hàng, xem đơn |

---

## 📂 Cấu trúc Database (Schema Overview)

Hệ thống sử dụng **7 bảng chính** được tối ưu hóa cho nghiệp vụ bán hàng:

1.  **Identity Tables** (`AspNetUsers`, `AspNetRoles`...): Quản lý người dùng & phân quyền.
2.  **Product Tables**:
    * `Categories`: Danh mục (Apple, Samsung...).
    * `Products`: Sản phẩm gốc (iPhone 15).
    * `ProductDetails`: Biến thể chi tiết (Màu, Ram, Giá, Tồn kho).
3.  **Order Tables**:
    * `OrderHeaders`: Thông tin chung đơn hàng.
    * `OrderDetails`: Chi tiết từng món hàng trong đơn.
4.  **Promotion Tables**: Quản lý mã giảm giá.

---

## 🤝 Đóng góp (Contribution)

Dự án được phát triển bởi **Team 204**:

1.  **Bùi Mạnh Tuấn** - *Backend Lead & Database Architect*: Xây dựng hệ thống lõi, API, Database.
2.  **Hoàng Văn An** - *Frontend Lead & UI/UX Designer*: Xây dựng giao diện, trải nghiệm người dùng.

---
Copyright © 2026 204Store Team.
Distributed under the MIT License.