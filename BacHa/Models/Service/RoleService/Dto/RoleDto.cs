using System.ComponentModel.DataAnnotations;

namespace BacHa.Models.Service.RoleService.Dto
{
    public class RoleDto
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public int UserCount { get; set; }
    }
}
