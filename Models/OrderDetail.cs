// Chi tiết đơn hàng
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _204Store.Models
{
    public class OrderDetail
    {
        [Key]
        public int Id { get; set; }

        public int OrderHeaderId { get; set; }
        [ForeignKey("OrderHeaderId")]
        public virtual OrderHeader OrderHeader { get; set; }

        public int ProductDetailId { get; set; }
        [ForeignKey("ProductDetailId")]
        public virtual ProductDetail ProductDetail { get; set; }

        public int Count { get; set; }
        public double Price { get; set; }
    }
}
