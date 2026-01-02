using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace _204Store.Models.ViewModels
{
    public class UserVM
    {
        public string Id { get; set; }

        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Số điện thoại")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Vai trò")]
        public string? Role { get; set; }

        [Display(Name = "Chọn vai trò mới")]
        public IEnumerable<SelectListItem>? RoleList { get; set; }

        public bool IsLocked { get; set; }
    }
}