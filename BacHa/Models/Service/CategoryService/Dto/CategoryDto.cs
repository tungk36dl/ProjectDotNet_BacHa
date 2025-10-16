using System;

namespace BacHa.Models.Service.CategoryService.Dto
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public Guid? ParentId { get; set; }
        public string? ParentName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ProductCount { get; set; }
    }
}
