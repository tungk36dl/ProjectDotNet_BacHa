using System;
using System.ComponentModel.DataAnnotations;

namespace BacHa.Models.Entity
{
    public class Product : BaseEntity
    {
        [Required(ErrorMessage = "Tên sản phẩm là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên sản phẩm không được vượt quá 100 ký tự")]
        public required string Name { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá sản phẩm là bắt buộc")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Số lượng tồn kho là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải lớn hơn hoặc bằng 0")]
        public int Stock { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500, ErrorMessage = "URL hình ảnh không được vượt quá 500 ký tự")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Thể loại là bắt buộc")]
        public Guid CategoryId { get; set; }

        // Navigation properties
        public virtual Category? Category { get; set; }
    }
}
