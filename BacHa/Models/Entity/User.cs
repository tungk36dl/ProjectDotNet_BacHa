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
    }
}
