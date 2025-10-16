using System.ComponentModel.DataAnnotations;

namespace BacHa.Models.Entity
{
    public class Category : BaseEntity
    {
        [Required(ErrorMessage = "Tên thể loại là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên thể loại không được vượt quá 100 ký tự")]
        public required string Name { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }

        public Guid? ParentId { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual Category? Parent { get; set; }
        public virtual ICollection<Category> Children { get; set; } = new List<Category>();
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
