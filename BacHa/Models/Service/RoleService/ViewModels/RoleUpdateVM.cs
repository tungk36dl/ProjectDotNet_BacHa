using System.ComponentModel.DataAnnotations;

namespace BacHa.Models.Service.RoleService.ViewModels
{
    public class RoleUpdateVM
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Role name is required")]
        [StringLength(50, ErrorMessage = "Role name cannot exceed 50 characters")]
        [Display(Name = "Role Name")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "Description cannot exceed 200 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }
}

