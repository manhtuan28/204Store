using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace _204Store.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [Display(Name = "Tên danh mục")]
        public string Name { get; set; }

        [Display(Name = "Thứ tự hiển thị")]
        [Range(1, 100, ErrorMessage = "Thứ tự phải từ 1 đến 100")]
        public int DisplayOrder { get; set; }

        [Display(Name = "Biểu tượng (Icon Class)")]
        public string? Icon { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Danh mục cha")]
        public int? ParentId { get; set; }

        [ForeignKey("ParentId")]
        public virtual Category? ParentCategory { get; set; }
        public string? Alias { get; set; }
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Category>? ChildCategories { get; set; }
    }
}