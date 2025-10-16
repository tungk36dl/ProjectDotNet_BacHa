using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BacHa.Models.UnitOfWork;
using BacHa.Models.Service.CategoryService.Dto;
using BacHa.Models.Entity;

namespace BacHa.Models.Service.CategoryService
{
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Category, Guid> _categoryRepository;
        private readonly ILogger<CategoryService> _logger;

        public CategoryService(IUnitOfWork unitOfWork, IGenericRepository<Category, Guid> categoryRepository, ILogger<CategoryService> logger)
        {
            _unitOfWork = unitOfWork;
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        public async Task<DataResponse<List<Category>>> GetAllAsync(CategorySearch? search = null)
        {
            try
            {
                IQueryable<Category> query = _categoryRepository.FindAll(null, c => c.Parent);

                if (search != null)
                {
                    if (!string.IsNullOrWhiteSpace(search.Query))
                    {
                        var qstr = search.Query.Trim();
                        query = query.Where(c => c.Name.Contains(qstr) || 
                                               (c.Description != null && c.Description.Contains(qstr)));
                    }

                    if (search.IsActive.HasValue)
                        query = query.Where(c => c.IsActive == search.IsActive.Value);

                    if (search.ParentId.HasValue)
                        query = query.Where(c => c.ParentId == search.ParentId.Value);

                    // paging
                    var skip = (Math.Max(1, search.Page) - 1) * Math.Max(1, search.PageSize);
                    query = query.Skip(skip).Take(Math.Max(1, search.PageSize));
                }

                var data = await query.OrderBy(c => c.Name).ToListAsync();
                return new DataResponse<List<Category>> { Success = true, Data = data };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                return new DataResponse<List<Category>> { Success = false, Message = "Failed to get categories.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<Category?>> GetByIdAsync(Guid id)
        {
            try
            {
                var category = await _categoryRepository.FindByIdAsync(id, c => c.Parent);
                return new DataResponse<Category?> { Success = true, Data = category };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category by id: {CategoryId}", id);
                return new DataResponse<Category?> { Success = false, Message = $"Failed to get category by id: {id}", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<Category>> AddAsync(Category category)
        {
            if (category == null)
            {
                return new DataResponse<Category> { Success = false, Message = "Category is required." };
            }

            try
            {
                // Validate data annotations
                var ctx = new ValidationContext(category);
                Validator.ValidateObject(category, ctx, validateAllProperties: true);

                // Check uniqueness of Name
                var fieldErrors = new Dictionary<string, List<string>>();
                if (!string.IsNullOrWhiteSpace(category.Name))
                {
                    var existsName = await _categoryRepository.AnyAsync(c => c.Name == category.Name);
                    if (existsName)
                    {
                        fieldErrors.TryAdd(nameof(category.Name), new List<string>());
                        fieldErrors[nameof(category.Name)].Add("Tên thể loại đã tồn tại.");
                    }
                }

                // Validate ParentId if provided
                if (category.ParentId.HasValue)
                {
                    var parentExists = await _categoryRepository.AnyAsync(c => c.Id == category.ParentId.Value);
                    if (!parentExists)
                    {
                        fieldErrors.TryAdd(nameof(category.ParentId), new List<string>());
                        fieldErrors[nameof(category.ParentId)].Add("Thể loại cha không tồn tại.");
                    }
                }

                if (fieldErrors.Any())
                {
                    return new DataResponse<Category>
                    {
                        Success = false,
                        Message = "Validation errors",
                        ErrorDetails = System.Text.Json.JsonSerializer.Serialize(fieldErrors)
                    };
                }

                if (category.Id == Guid.Empty) category.Id = Guid.NewGuid();
                category.CreatedAt = DateTime.UtcNow;
                category.UpdatedAt = DateTime.UtcNow;

                _categoryRepository.Add(category);
                await _unitOfWork.SaveChangesAsync();
                return new DataResponse<Category> { Success = true, Data = category };
            }
            catch (ValidationException vex)
            {
                return new DataResponse<Category> { Success = false, Message = vex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding category");
                return new DataResponse<Category> { Success = false, Message = "Failed to add category.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<Category>> UpdateAsync(Category category)
        {
            if (category == null)
            {
                return new DataResponse<Category> { Success = false, Message = "Category is required." };
            }

            try
            {
                var ctx = new ValidationContext(category);
                Validator.ValidateObject(category, ctx, validateAllProperties: true);

                var fieldErrors = new Dictionary<string, List<string>>();
                // Check uniqueness excluding current category
                if (!string.IsNullOrWhiteSpace(category.Name))
                {
                    var existsName = await _categoryRepository.AnyAsync(c => c.Id != category.Id && c.Name == category.Name);
                    if (existsName)
                    {
                        fieldErrors.TryAdd(nameof(category.Name), new List<string>());
                        fieldErrors[nameof(category.Name)].Add("Tên thể loại đã tồn tại.");
                    }
                }

                // Validate ParentId if provided
                if (category.ParentId.HasValue)
                {
                    if (category.ParentId.Value == category.Id)
                    {
                        fieldErrors.TryAdd(nameof(category.ParentId), new List<string>());
                        fieldErrors[nameof(category.ParentId)].Add("Thể loại không thể là cha của chính nó.");
                    }
                    else
                    {
                        var parentExists = await _categoryRepository.AnyAsync(c => c.Id == category.ParentId.Value);
                        if (!parentExists)
                        {
                            fieldErrors.TryAdd(nameof(category.ParentId), new List<string>());
                            fieldErrors[nameof(category.ParentId)].Add("Thể loại cha không tồn tại.");
                        }
                    }
                }

                if (fieldErrors.Any())
                {
                    return new DataResponse<Category>
                    {
                        Success = false,
                        Message = "Validation errors",
                        ErrorDetails = System.Text.Json.JsonSerializer.Serialize(fieldErrors)
                    };
                }

                category.UpdatedAt = DateTime.UtcNow;
                _categoryRepository.Update(category);
                await _unitOfWork.SaveChangesAsync();
                return new DataResponse<Category> { Success = true, Data = category };
            }
            catch (ValidationException vex)
            {
                return new DataResponse<Category> { Success = false, Message = vex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category");
                return new DataResponse<Category> { Success = false, Message = "Failed to update category.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<object>> DeleteAsync(Guid id)
        {
            try
            {
                var category = await _categoryRepository.FindByIdAsync(id);
                if (category == null)
                {
                    return new DataResponse<object> { Success = false, Message = "Category not found." };
                }

                // Check if category has child categories
                var childCount = await _categoryRepository.FindAll(c => c.ParentId == id).CountAsync();
                if (childCount > 0)
                {
                    return new DataResponse<object> 
                    { 
                        Success = false, 
                        Message = $"Không thể xóa thể loại. Có {childCount} thể loại con đang sử dụng thể loại này." 
                    };
                }

                // Check if category has products (we'll need to add this check when Product entity is available)
                // For now, we'll just delete the category
                _categoryRepository.Remove(category);
                await _unitOfWork.SaveChangesAsync();
                return new DataResponse<object> { Success = true, Data = null };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category: {CategoryId}", id);
                return new DataResponse<object> { Success = false, Message = $"Failed to delete category: {id}", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<List<CategoryDto>>> GetAllWithProductCountAsync()
        {
            try
            {
                var categories = await _categoryRepository.FindAll()
                    .Select(c => new CategoryDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Description = c.Description,
                        ParentId = c.ParentId,
                        ParentName = c.Parent != null ? c.Parent.Name : null,
                        IsActive = c.IsActive,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        ProductCount = 0 // Will be updated when Product entity is available
                    })
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return new DataResponse<List<CategoryDto>> { Success = true, Data = categories };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories with product count");
                return new DataResponse<List<CategoryDto>> { Success = false, Message = "Failed to get categories.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<bool>> IsNameExistsAsync(string name, Guid? excludeId = null)
        {
            try
            {
                var exists = await _categoryRepository.AnyAsync(c => c.Name == name && (excludeId == null || c.Id != excludeId));
                return new DataResponse<bool> { Success = true, Data = exists };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking category name existence");
                return new DataResponse<bool> { Success = false, Message = "Failed to check category name.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<List<Category>>> GetActiveCategoriesAsync()
        {
            try
            {
                var categories = await _categoryRepository.FindAll(c => c.IsActive == true, c => c.Parent)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return new DataResponse<List<Category>> { Success = true, Data = categories };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active categories");
                return new DataResponse<List<Category>> { Success = false, Message = "Failed to get active categories.", ErrorDetails = ex.Message };
            }
        }
    }
}
