// Sản phẩm
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _204Store.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }

        [Display(Name = "Sản phẩm nổi bật")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Giảm giá (%)")]
        [Range(0, 100, ErrorMessage = "Giảm giá từ 0% đến 100%")]
        public double DiscountRate { get; set; } = 0;

        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }
        public virtual ICollection<ProductDetail>? ProductDetails { get; set; }
        [Display(Name = "Đã bán")]
        public int Sold { get; set; } = 0;
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public string? Specifications { get; set; }
    }
}