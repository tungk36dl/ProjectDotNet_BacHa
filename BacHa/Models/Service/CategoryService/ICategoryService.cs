using BacHa.Models.Entity;
using BacHa.Models.Service.CategoryService.Dto;

namespace BacHa.Models.Service.CategoryService
{
    public interface ICategoryService
    {
        Task<DataResponse<List<Category>>> GetAllAsync(CategorySearch? search = null);
        Task<DataResponse<Category?>> GetByIdAsync(Guid id);
        Task<DataResponse<Category>> AddAsync(Category category);
        Task<DataResponse<Category>> UpdateAsync(Category category);
        Task<DataResponse<object>> DeleteAsync(Guid id);
        Task<DataResponse<List<CategoryDto>>> GetAllWithProductCountAsync();
        Task<DataResponse<bool>> IsNameExistsAsync(string name, Guid? excludeId = null);
        Task<DataResponse<List<Category>>> GetActiveCategoriesAsync();
    }
}
