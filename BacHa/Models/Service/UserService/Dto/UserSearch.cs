namespace BacHa.Models.Service.UserService.Dto
{
    using BacHa.Models.Service;

    public class UserSearch : SearchBase
    {
        public string? Query { get; set; }
        public bool? IsActive { get; set; }
    }
}
