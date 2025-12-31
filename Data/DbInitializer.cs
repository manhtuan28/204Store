using _204Store.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace _204Store.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            context.Database.EnsureCreated();

            if (context.Products.Any())
            {
                return;
            }

            var roles = new[] { "Admin", "Customer" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminEmail = "admin@gmail.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Quản Trị Viên",
                    Address = "Hệ thống 204Store",
                    CreatedDate = DateTime.Now,
                    EmailConfirmed = true,
                    PhoneNumber = "0988888888"
                };
                // Mật khẩu mẫu: Admin@123
                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }

            var categories = new Category[]
            {
                new Category { Name = "Điện thoại" },
                new Category { Name = "Laptop" },
                new Category { Name = "Đồng hồ thông minh" },
                new Category { Name = "Tai nghe & Phụ kiện" }
            };
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();

            var iphone15 = new Product
            {
                Name = "iPhone 15 Pro Max",
                Description = "Thiết kế Titan tự nhiên bền bỉ, chip A17 Pro mạnh mẽ nhất.",
                ImageUrl = "https://cdn.tgdd.vn/Products/Images/42/305658/iphone-15-pro-max-blue-thumbnew-600x600.jpg",
                CategoryId = categories[0].Id,
                DiscountRate = 10,
                IsFeatured = true
            };
            context.Products.Add(iphone15);
            await context.SaveChangesAsync();

            var iphoneDetails = new ProductDetail[]
            {
                new ProductDetail { ProductId = iphone15.Id, Color = "Titan Tự Nhiên", Ram = "8GB", Rom = "256GB", Price = 29990000m, Quantity = 50 },
                new ProductDetail { ProductId = iphone15.Id, Color = "Titan Xanh", Ram = "8GB", Rom = "512GB", Price = 35990000m, Quantity = 30 },
                new ProductDetail { ProductId = iphone15.Id, Color = "Titan Đen", Ram = "1TB", Rom = "1TB", Price = 41990000m, Quantity = 10 }
            };
            context.ProductDetails.AddRange(iphoneDetails);


            var s24 = new Product
            {
                Name = "Samsung Galaxy S24 Ultra 5G",
                Description = "Quyền năng Galaxy AI, camera mắt thần bóng đêm.",
                ImageUrl = "https://cdn.tgdd.vn/Products/Images/42/307172/samsung-galaxy-s24-ultra-grey-thumbnew-600x600.jpg",
                CategoryId = categories[0].Id,
                DiscountRate = 5,
                IsFeatured = true
            };
            context.Products.Add(s24);
            await context.SaveChangesAsync();

            context.ProductDetails.AddRange(new[]
            {
                new ProductDetail { ProductId = s24.Id, Color = "Xám Titan", Ram = "12GB", Rom = "256GB", Price = 26990000m, Quantity = 40 },
                new ProductDetail { ProductId = s24.Id, Color = "Vàng Titan", Ram = "12GB", Rom = "512GB", Price = 29990000m, Quantity = 20 }
            });


            var macbook = new Product
            {
                Name = "MacBook Air 13 inch M3",
                Description = "Siêu mỏng, siêu nhẹ, chip M3 cân mọi tác vụ văn phòng.",
                ImageUrl = "https://cdn.tgdd.vn/Products/Images/44/322609/macbook-air-13-inch-m3-midnight-thumb-600x600.jpg",
                CategoryId = categories[1].Id,
                DiscountRate = 0,
                IsFeatured = true
            };
            context.Products.Add(macbook);
            await context.SaveChangesAsync();

            context.ProductDetails.AddRange(new[]
            {
                new ProductDetail { ProductId = macbook.Id, Color = "Midnight", Ram = "8GB", Rom = "256GB", Price = 27990000m, Quantity = 100 },
                new ProductDetail { ProductId = macbook.Id, Color = "Starlight", Ram = "16GB", Rom = "512GB", Price = 32990000m, Quantity = 50 }
            });

            await context.SaveChangesAsync();
        }
    }
}