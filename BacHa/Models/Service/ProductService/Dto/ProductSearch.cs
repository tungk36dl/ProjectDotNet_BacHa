using BacHa.Models.Service;

namespace BacHa.Models.Service.ProductService.Dto
{
    public class ProductSearch : SearchBase
    {
        public bool? IsActive { get; set; }
        public Guid? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinStock { get; set; }
        public int? MaxStock { get; set; }
    }
}
