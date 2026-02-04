using System.ComponentModel.DataAnnotations.Schema;

namespace _204Store.Models
{
    public class ProductCategory
    {
        public int ProductId { get; set; }
        
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        public int CategoryId { get; set; }
        
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; } = null!;
    }
}
