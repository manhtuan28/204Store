using System.ComponentModel.DataAnnotations;

namespace _204Store.Models
{
    public class SystemSetting
    {
        [Key]
        [StringLength(100)]
        public string Key { get; set; }

        public string? Value { get; set; }
    }
}
