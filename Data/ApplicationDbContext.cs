using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using _204Store.Models;

namespace _204Store.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductDetail> ProductDetails { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<OrderHeader> OrderHeaders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<Promotion> Promotions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<OrderDetail>().HasKey(m => m.Id);

            builder.Entity<Category>().HasKey(m => m.Id);
            builder.Entity<Product>().HasKey(m => m.Id);
            builder.Entity<ProductDetail>().HasKey(m => m.Id);
            builder.Entity<OrderHeader>().HasKey(m => m.Id);
            builder.Entity<Promotion>().HasKey(m => m.Id);
        }
    }
}