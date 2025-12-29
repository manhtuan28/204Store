using Microsoft.AspNetCore.Identity;

// User
namespace _204Store.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }

        public string? Address { get; set; }
        public DateTime? CreatedDate { get; set; }
    }
}
