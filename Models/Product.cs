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
        [Display(Name = "Tên sản phẩm")]
        public string Name { get; set; }

        public string Description { get; set; }
        public string ImageUrl { get; set; }

        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        public virtual ICollection<ProductDetail> ProductDetails { get; set; }
    }
}
