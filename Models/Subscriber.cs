using System.ComponentModel.DataAnnotations;

namespace _204Store.Models
{
    public class Subscriber
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        public DateTime SubscribeDate { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}