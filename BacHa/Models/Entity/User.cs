using System.ComponentModel.DataAnnotations;
using BacHa.Models.Entity;

namespace BacHa.Models
{
    public class User : BaseEntity
    {
    [Required]
    [StringLength(100)]
    public string? UserName { get; set; }

    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [StringLength(100)]
    public string? FullName { get; set; }

    public bool IsActive { get; set; } = true;
    
    // Hashed password (store salted+hashed value)
    public string? PasswordHash { get; set; }

    // Foreign key to Role
    public Guid? RoleId { get; set; }
    public virtual Role? Role { get; set; }

    // Legacy role field for backward compatibility (will be removed later)
    public string? RoleName { get; set; }
    }
}
