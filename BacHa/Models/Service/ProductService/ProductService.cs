using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BacHa.Models.UnitOfWork;
using BacHa.Models.Service.ProductService.Dto;
using BacHa.Models.Entity;

namespace BacHa.Models.Service.ProductService
{
    public class ProductService : IProductService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Product, Guid> _productRepository;
        private readonly ILogger<ProductService> _logger;

        public ProductService(IUnitOfWork unitOfWork, IGenericRepository<Product, Guid> productRepository, ILogger<ProductService> logger)
        {
            _unitOfWork = unitOfWork;
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<DataResponse<List<Product>>> GetAllAsync(ProductSearch? search = null)
        {
            try
            {
                IQueryable<Product> query = _productRepository.FindAll(null, p => p.Category);

                if (search != null)
                {
                    if (!string.IsNullOrWhiteSpace(search.Query))
                    {
                        var qstr = search.Query.Trim();
                        query = query.Where(p => p.Name.Contains(qstr) || 
                                               (p.Description != null && p.Description.Contains(qstr)));
                    }

                    if (search.IsActive.HasValue)
                        query = query.Where(p => p.IsActive == search.IsActive.Value);

                    if (search.CategoryId.HasValue)
                        query = query.Where(p => p.CategoryId == search.CategoryId.Value);

                    if (search.MinPrice.HasValue)
                        query = query.Where(p => p.Price >= search.MinPrice.Value);

                    if (search.MaxPrice.HasValue)
                        query = query.Where(p => p.Price <= search.MaxPrice.Value);

                    if (search.MinStock.HasValue)
                        query = query.Where(p => p.Stock >= search.MinStock.Value);

                    if (search.MaxStock.HasValue)
                        query = query.Where(p => p.Stock <= search.MaxStock.Value);

                    // paging
                    var skip = (Math.Max(1, search.Page) - 1) * Math.Max(1, search.PageSize);
                    query = query.Skip(skip).Take(Math.Max(1, search.PageSize));
                }

                var data = await query.OrderBy(p => p.Name).ToListAsync();
                return new DataResponse<List<Product>> { Success = true, Data = data };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return new DataResponse<List<Product>> { Success = false, Message = "Failed to get products.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<Product?>> GetByIdAsync(Guid id)
        {
            try
            {
                var product = await _productRepository.FindByIdAsync(id, p => p.Category);
                return new DataResponse<Product?> { Success = true, Data = product };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product by id: {ProductId}", id);
                return new DataResponse<Product?> { Success = false, Message = $"Failed to get product by id: {id}", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<Product>> AddAsync(Product product)
        {
            if (product == null)
            {
                return new DataResponse<Product> { Success = false, Message = "Product is required." };
            }

            try
            {
                // Validate data annotations
                var ctx = new ValidationContext(product);
                Validator.ValidateObject(product, ctx, validateAllProperties: true);

                // Check uniqueness of Name
                var fieldErrors = new Dictionary<string, List<string>>();
                if (!string.IsNullOrWhiteSpace(product.Name))
                {
                    var existsName = await _productRepository.AnyAsync(p => p.Name == product.Name);
                    if (existsName)
                    {
                        fieldErrors.TryAdd(nameof(product.Name), new List<string>());
                        fieldErrors[nameof(product.Name)].Add("Tên sản phẩm đã tồn tại.");
                    }
                }

                // Validate CategoryId exists - we'll need to check this in CategoryService
                // For now, we'll assume it exists if CategoryId is provided

                if (fieldErrors.Any())
                {
                    return new DataResponse<Product>
                    {
                        Success = false,
                        Message = "Validation errors",
                        ErrorDetails = System.Text.Json.JsonSerializer.Serialize(fieldErrors)
                    };
                }

                if (product.Id == Guid.Empty) product.Id = Guid.NewGuid();
                product.CreatedAt = DateTime.UtcNow;
                product.UpdatedAt = DateTime.UtcNow;

                _productRepository.Add(product);
                await _unitOfWork.SaveChangesAsync();
                return new DataResponse<Product> { Success = true, Data = product };
            }
            catch (ValidationException vex)
            {
                return new DataResponse<Product> { Success = false, Message = vex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product");
                return new DataResponse<Product> { Success = false, Message = "Failed to add product.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<Product>> UpdateAsync(Product product)
        {
            if (product == null)
            {
                return new DataResponse<Product> { Success = false, Message = "Product is required." };
            }

            try
            {
                var ctx = new ValidationContext(product);
                Validator.ValidateObject(product, ctx, validateAllProperties: true);

                var fieldErrors = new Dictionary<string, List<string>>();
                // Check uniqueness excluding current product
                if (!string.IsNullOrWhiteSpace(product.Name))
                {
                    var existsName = await _productRepository.AnyAsync(p => p.Id != product.Id && p.Name == product.Name);
                    if (existsName)
                    {
                        fieldErrors.TryAdd(nameof(product.Name), new List<string>());
                        fieldErrors[nameof(product.Name)].Add("Tên sản phẩm đã tồn tại.");
                    }
                }

                if (fieldErrors.Any())
                {
                    return new DataResponse<Product>
                    {
                        Success = false,
                        Message = "Validation errors",
                        ErrorDetails = System.Text.Json.JsonSerializer.Serialize(fieldErrors)
                    };
                }

                product.UpdatedAt = DateTime.UtcNow;
                _productRepository.Update(product);
                await _unitOfWork.SaveChangesAsync();
                return new DataResponse<Product> { Success = true, Data = product };
            }
            catch (ValidationException vex)
            {
                return new DataResponse<Product> { Success = false, Message = vex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product");
                return new DataResponse<Product> { Success = false, Message = "Failed to update product.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<object>> DeleteAsync(Guid id)
        {
            try
            {
                var product = await _productRepository.FindByIdAsync(id);
                if (product == null)
                {
                    return new DataResponse<object> { Success = false, Message = "Product not found." };
                }

                _productRepository.Remove(product);
                await _unitOfWork.SaveChangesAsync();
                return new DataResponse<object> { Success = true, Data = null };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product: {ProductId}", id);
                return new DataResponse<object> { Success = false, Message = $"Failed to delete product: {id}", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<List<ProductDto>>> GetAllWithCategoryAsync()
        {
            try
            {
                var products = await _productRepository.FindAll(null, p => p.Category)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        Stock = p.Stock,
                        IsActive = p.IsActive,
                        ImageUrl = p.ImageUrl,
                        CategoryId = p.CategoryId,
                        CategoryName = p.Category != null ? p.Category.Name : "N/A",
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                return new DataResponse<List<ProductDto>> { Success = true, Data = products };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products with category");
                return new DataResponse<List<ProductDto>> { Success = false, Message = "Failed to get products.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<bool>> IsNameExistsAsync(string name, Guid? excludeId = null)
        {
            try
            {
                var exists = await _productRepository.AnyAsync(p => p.Name == name && (excludeId == null || p.Id != excludeId));
                return new DataResponse<bool> { Success = true, Data = exists };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking product name existence");
                return new DataResponse<bool> { Success = false, Message = "Failed to check product name.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<List<Product>>> GetActiveProductsAsync()
        {
            try
            {
                var products = await _productRepository.FindAll(p => p.IsActive == true, p => p.Category)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                return new DataResponse<List<Product>> { Success = true, Data = products };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active products");
                return new DataResponse<List<Product>> { Success = false, Message = "Failed to get active products.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<List<Product>>> GetProductsByCategoryAsync(Guid categoryId)
        {
            try
            {
                var products = await _productRepository.FindAll(p => p.CategoryId == categoryId, p => p.Category)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                return new DataResponse<List<Product>> { Success = true, Data = products };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products by category: {CategoryId}", categoryId);
                return new DataResponse<List<Product>> { Success = false, Message = "Failed to get products by category.", ErrorDetails = ex.Message };
            }
        }
    }
}
