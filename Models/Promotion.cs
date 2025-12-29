// Khuyến mãi
using System.ComponentModel.DataAnnotations;

namespace _204Store.Models
{
    public class Promotion
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Code { get; set; }
        public double DiscountPercent { get; set; }
        public double DiscountAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
