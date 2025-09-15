using BacHa.Models.Service;

namespace BacHa.Models.Service.RoleService.Dto
{
    public class RoleSearch : SearchBase
    {
        public string? Query { get; set; }
        public bool? IsActive { get; set; }
    }
}
