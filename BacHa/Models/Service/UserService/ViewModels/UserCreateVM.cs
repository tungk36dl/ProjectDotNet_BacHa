namespace BacHa.Models.Service.UserService.ViewModels
{
    public class UserCreateVM
    {
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Password { get; set; }
        public string? Role { get; set; }
    }
}
