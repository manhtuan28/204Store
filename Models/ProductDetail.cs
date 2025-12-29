// Chi tiết sản phẩm
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _204Store.Models
{
    public class ProductDetail
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; }

        public string Color { get; set; }
        public string Ram { get; set; }
        public string Rom { get; set; }

        [Required]
        public double Price { get; set; }

        [Required]
        public int Quantity { get; set; }
    }
}
