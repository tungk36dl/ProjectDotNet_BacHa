using System.ComponentModel.DataAnnotations;

namespace BacHa.Models.Service.UserService.Dto
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }

        public string? FullName { get; set; }

        public bool? IsActive { get; set; }

        public string? PasswordHash { get; set; }

        public string? Role { get; set; }
    }
}
