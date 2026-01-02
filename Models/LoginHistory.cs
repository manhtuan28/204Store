using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _204Store.Models
{
    public class LoginHistory
    {
        [Key]
        public int Id { get; set; }

        public string ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        public ApplicationUser ApplicationUser { get; set; }

        public DateTime LoginTime { get; set; } = DateTime.Now;
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
        public string? Location { get; set; }
    }
}