using System.ComponentModel.DataAnnotations;

namespace BacHa.Models.Service.UserService.ViewModels
{
    public class UserCreateVM
    {
        [Required]
        [StringLength(100)]
        public string? UserName { get; set; }
        [Required]
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;
        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string? Password { get; set; }
        public string? Role { get; set; }
    }
}
