using Microsoft.AspNetCore.Mvc;
using BacHa.Models;
using BacHa.Models.Entity;
using BacHa.Models.Service;
using BacHa.Models.Service.ProductService;
using BacHa.Models.Service.ProductService.Dto;
using BacHa.Models.Service.ProductService.ViewModels;
using BacHa.Models.Service.CategoryService;
using BacHa.Helper;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace BacHa.Controllers
{
    [Authorize]
    public class ProductsController : BaseController
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(IProductService productService, ICategoryService categoryService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _categoryService = categoryService;
            _logger = logger;
        }

        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Index([FromQuery] ProductSearch? search)
        {
            // Load categories for filter dropdown
            var categoriesResp = await _categoryService.GetActiveCategoriesAsync();
            ViewBag.Categories = categoriesResp.Success ? categoriesResp.Data : new List<Category>();
            
            var resp = await _productService.GetAllAsync(search);
            if (!resp.Success)
            {
                ViewBag.ErrorMessage = resp.Message ?? resp.ErrorDetails;
                ViewBag.Search = search ?? new ProductSearch();
                return View(new List<Product>());
            }
            ViewBag.Search = search ?? new ProductSearch();
            return View(resp.Data ?? new List<Product>());
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var resp = await _productService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null) return NotFound();
            return View(resp.Data);
        }
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Create()
        {
            // Load categories for dropdown
            var categoriesResp = await _categoryService.GetActiveCategoriesAsync();
            ViewBag.Categories = categoriesResp.Success ? categoriesResp.Data : new List<Category>();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Create(ProductCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                // Reload categories for dropdown
                var categoriesResp = await _categoryService.GetActiveCategoriesAsync();
                ViewBag.Categories = categoriesResp.Success ? categoriesResp.Data : new List<Category>();
                return View(model);
            }

            var product = new Product
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                Stock = model.Stock,
                IsActive = model.IsActive,
                ImageUrl = model.ImageUrl,
                CategoryId = model.CategoryId
            };

            var result = await _productService.AddAsync(product);
            if (!result.Success)
            {
                ModelState.AddDataResponse(new DataResponse<object> { Success = result.Success, Message = result.Message, ErrorDetails = result.ErrorDetails });
                // Reload categories for dropdown
                var categoriesResp = await _categoryService.GetActiveCategoriesAsync();
                ViewBag.Categories = categoriesResp.Success ? categoriesResp.Data : new List<Category>();
                return View(model);
            }

            TempData["SuccessMessage"] = "Sản phẩm đã được tạo thành công.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var resp = await _productService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null) return NotFound();

            var model = new ProductUpdateVM
            {
                Id = resp.Data.Id,
                Name = resp.Data.Name,
                Description = resp.Data.Description,
                Price = resp.Data.Price,
                Stock = resp.Data.Stock,
                IsActive = resp.Data.IsActive,
                ImageUrl = resp.Data.ImageUrl,
                CategoryId = resp.Data.CategoryId
            };

            // Load categories for dropdown
            var categoriesResp = await _categoryService.GetActiveCategoriesAsync();
            ViewBag.Categories = categoriesResp.Success ? categoriesResp.Data : new List<Category>();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Edit(Guid id, ProductUpdateVM model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                // Reload categories for dropdown
                var categoriesResp = await _categoryService.GetActiveCategoriesAsync();
                ViewBag.Categories = categoriesResp.Success ? categoriesResp.Data : new List<Category>();
                return View(model);
            }

            var product = new Product
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                Stock = model.Stock,
                IsActive = model.IsActive,
                ImageUrl = model.ImageUrl,
                CategoryId = model.CategoryId
            };

            var resp = await _productService.UpdateAsync(product);
            if (!resp.Success)
            {
                ModelState.AddDataResponse(new DataResponse<object> { Success = resp.Success, Message = resp.Message, ErrorDetails = resp.ErrorDetails });
                // Reload categories for dropdown
                var categoriesResp = await _categoryService.GetActiveCategoriesAsync();
                ViewBag.Categories = categoriesResp.Success ? categoriesResp.Data : new List<Category>();
                return View(model);
            }

            TempData["SuccessMessage"] = "Sản phẩm đã được cập nhật thành công.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var resp = await _productService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null) return NotFound();
            return View(resp.Data);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,NhanVien")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var resp = await _productService.DeleteAsync(id);
            if (!resp.Success)
            {
                TempData["ErrorMessage"] = resp.Message ?? resp.ErrorDetails;
            }
            else
            {
                TempData["SuccessMessage"] = "Sản phẩm đã được xóa thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CheckNameExists(string name, Guid? excludeId = null)
        {
            var resp = await _productService.IsNameExistsAsync(name, excludeId);
            if (resp.Success)
            {
                return Json(new { exists = resp.Data });
            }
            return Json(new { exists = false });
        }

        [HttpGet]
        public async Task<IActionResult> GetProductsWithCategory()
        {
            var resp = await _productService.GetAllWithCategoryAsync();
            if (resp.Success)
            {
                return Json(new { success = true, data = resp.Data });
            }
            return Json(new { success = false, message = resp.Message });
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveProducts()
        {
            var resp = await _productService.GetActiveProductsAsync();
            if (resp.Success)
            {
                return Json(new { success = true, data = resp.Data });
            }
            return Json(new { success = false, message = resp.Message });
        }

        [HttpGet]
        public async Task<IActionResult> GetProductsByCategory(Guid categoryId)
        {
            var resp = await _productService.GetProductsByCategoryAsync(categoryId);
            if (resp.Success)
            {
                return Json(new { success = true, data = resp.Data });
            }
            return Json(new { success = false, message = resp.Message });
        }

        // Public actions for users (no authorization required)
        [AllowAnonymous]
        public async Task<IActionResult> Shop([FromQuery] ProductSearch? search)
        {
            // Only show active products for public shop
            if (search == null) search = new ProductSearch();
            search.IsActive = true;

            // Load categories for filter dropdown
            var categoriesResp = await _categoryService.GetActiveCategoriesAsync();
            ViewBag.Categories = categoriesResp.Success ? categoriesResp.Data : new List<Category>();
            
            var resp = await _productService.GetAllAsync(search);
            if (!resp.Success)
            {
                ViewBag.ErrorMessage = resp.Message ?? resp.ErrorDetails;
                ViewBag.Search = search;
                return View(new List<Product>());
            }
            ViewBag.Search = search;
            return View(resp.Data ?? new List<Product>());
        }

        [AllowAnonymous]
        public async Task<IActionResult> ProductDetails(Guid id)
        {
            var resp = await _productService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null || !resp.Data.IsActive) 
                return NotFound();
            
            // Lưu lịch sử xem sản phẩm vào Session
            SaveProductToViewHistory(id);
            
            return View(resp.Data);
        }


        private void SaveProductToViewHistory(Guid productId)
        {
            var viewedProductIds = GetViewedProductIds();
            
            // Loại bỏ sản phẩm nếu đã tồn tại (để đưa lên đầu danh sách)
            viewedProductIds.Remove(productId);
            
            // Thêm sản phẩm vào đầu danh sách
            viewedProductIds.Insert(0, productId);
            
            // Giới hạn tối đa 20 sản phẩm trong lịch sử
            if (viewedProductIds.Count > 20)
            {
                viewedProductIds = viewedProductIds.Take(20).ToList();
            }
            
            // Lưu vào Session
            HttpContext.Session.SetObjectAsJson("ViewedProductIds", viewedProductIds);
        }

        private List<Guid> GetViewedProductIds()
        {
            return HttpContext.Session.GetObjectFromJson<List<Guid>>("ViewedProductIds") ?? new List<Guid>();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetViewHistory()
        {
            try
            {
                var viewedProductIds = GetViewedProductIds();
                if (!viewedProductIds.Any())
                {
                    return Json(new { success = true, data = new List<object>() });
                }

                var products = new List<object>();
                foreach (var productId in viewedProductIds)
                {
                    var resp = await _productService.GetByIdAsync(productId);
                    if (resp.Success && resp.Data != null && resp.Data.IsActive)
                    {
                        products.Add(new
                        {
                            id = resp.Data.Id,
                            name = resp.Data.Name,
                            price = resp.Data.Price,
                            imageUrl = resp.Data.ImageUrl,
                            stock = resp.Data.Stock
                        });
                    }
                }

                return Json(new { success = true, data = products });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải lịch sử: " + ex.Message });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult ClearViewHistory()
        {
            try
            {
                HttpContext.Session.Remove("ViewedProductIds");
                return Json(new { success = true, message = "Đã xóa lịch sử xem sản phẩm thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi xóa lịch sử: " + ex.Message });
            }
        }
    }
}