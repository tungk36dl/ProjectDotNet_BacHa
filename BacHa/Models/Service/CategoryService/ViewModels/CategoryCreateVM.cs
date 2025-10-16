using System.ComponentModel.DataAnnotations;

namespace BacHa.Models.Service.CategoryService.ViewModels
{
    public class CategoryCreateVM
    {
        [Required(ErrorMessage = "Tên thể loại là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên thể loại không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }

        public Guid? ParentId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
