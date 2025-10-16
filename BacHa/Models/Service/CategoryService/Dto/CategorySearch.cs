using BacHa.Models.Service;

namespace BacHa.Models.Service.CategoryService.Dto
{
    public class CategorySearch : SearchBase
    {
        public bool? IsActive { get; set; }
        public Guid? ParentId { get; set; }
    }
}
