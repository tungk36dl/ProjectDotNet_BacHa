using BacHa.Models.Entity;
using BacHa.Models.Service.ProductService.Dto;

namespace BacHa.Models.Service.ProductService
{
    public interface IProductService
    {
        Task<DataResponse<List<Product>>> GetAllAsync(ProductSearch? search = null);
        Task<DataResponse<Product?>> GetByIdAsync(Guid id);
        Task<DataResponse<Product>> AddAsync(Product product);
        Task<DataResponse<Product>> UpdateAsync(Product product);
        Task<DataResponse<object>> DeleteAsync(Guid id);
        Task<DataResponse<List<ProductDto>>> GetAllWithCategoryAsync();
        Task<DataResponse<bool>> IsNameExistsAsync(string name, Guid? excludeId = null);
        Task<DataResponse<List<Product>>> GetActiveProductsAsync();
        Task<DataResponse<List<Product>>> GetProductsByCategoryAsync(Guid categoryId);
    }
}
