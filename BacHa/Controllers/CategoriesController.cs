using Microsoft.AspNetCore.Mvc;
using BacHa.Models;
using BacHa.Models.Entity;
using BacHa.Models.Service;
using BacHa.Models.Service.CategoryService;
using BacHa.Models.Service.CategoryService.Dto;
using BacHa.Models.Service.CategoryService.ViewModels;
using BacHa.Helper;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace BacHa.Controllers
{
    [Authorize]
    public class CategoriesController : BaseController
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ICategoryService categoryService, ILogger<CategoriesController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        public async Task<IActionResult> Index([FromQuery] CategorySearch? search)
        {
            var resp = await _categoryService.GetAllAsync(search);
            if (!resp.Success)
            {
                ViewBag.ErrorMessage = resp.Message ?? resp.ErrorDetails;
                ViewBag.Search = search ?? new CategorySearch();
                return View(new List<Category>());
            }
            ViewBag.Search = search ?? new CategorySearch();
            return View(resp.Data ?? new List<Category>());
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var resp = await _categoryService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null) return NotFound();
            return View(resp.Data);
        }

        public async Task<IActionResult> Create()
        {
            // Load parent categories for dropdown
            var parentCategoriesResp = await _categoryService.GetActiveCategoriesAsync();
            ViewBag.ParentCategories = parentCategoriesResp.Success ? parentCategoriesResp.Data : new List<Category>();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                // Reload parent categories for dropdown
                var parentCategoriesResp = await _categoryService.GetActiveCategoriesAsync();
                ViewBag.ParentCategories = parentCategoriesResp.Success ? parentCategoriesResp.Data : new List<Category>();
                return View(model);
            }

            var category = new Category
            {
                Name = model.Name,
                Description = model.Description,
                ParentId = model.ParentId,
                IsActive = model.IsActive
            };

            var result = await _categoryService.AddAsync(category);
            if (!result.Success)
            {
                ModelState.AddDataResponse(new DataResponse<object> { Success = result.Success, Message = result.Message, ErrorDetails = result.ErrorDetails });
                // Reload parent categories for dropdown
                var parentCategoriesResp = await _categoryService.GetActiveCategoriesAsync();
                ViewBag.ParentCategories = parentCategoriesResp.Success ? parentCategoriesResp.Data : new List<Category>();
                return View(model);
            }

            TempData["SuccessMessage"] = "Thể loại đã được tạo thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var resp = await _categoryService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null) return NotFound();

            var model = new CategoryUpdateVM
            {
                Id = resp.Data.Id,
                Name = resp.Data.Name,
                Description = resp.Data.Description,
                ParentId = resp.Data.ParentId,
                IsActive = resp.Data.IsActive
            };

            // Load parent categories for dropdown (excluding current category)
            var parentCategoriesResp = await _categoryService.GetActiveCategoriesAsync();
            var parentCategories = parentCategoriesResp.Success ? parentCategoriesResp.Data : new List<Category>();
            ViewBag.ParentCategories = parentCategories.Where(c => c.Id != id).ToList();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CategoryUpdateVM model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid)
            {
                // Reload parent categories for dropdown (excluding current category)
                var parentCategoriesResp = await _categoryService.GetActiveCategoriesAsync();
                var parentCategories = parentCategoriesResp.Success ? parentCategoriesResp.Data : new List<Category>();
                ViewBag.ParentCategories = parentCategories.Where(c => c.Id != id).ToList();
                return View(model);
            }

            var category = new Category
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                ParentId = model.ParentId,
                IsActive = model.IsActive
            };

            var resp = await _categoryService.UpdateAsync(category);
            if (!resp.Success)
            {
                ModelState.AddDataResponse(new DataResponse<object> { Success = resp.Success, Message = resp.Message, ErrorDetails = resp.ErrorDetails });
                // Reload parent categories for dropdown (excluding current category)
                var parentCategoriesResp = await _categoryService.GetActiveCategoriesAsync();
                var parentCategories = parentCategoriesResp.Success ? parentCategoriesResp.Data : new List<Category>();
                ViewBag.ParentCategories = parentCategories.Where(c => c.Id != id).ToList();
                return View(model);
            }

            TempData["SuccessMessage"] = "Thể loại đã được cập nhật thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var resp = await _categoryService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null) return NotFound();
            return View(resp.Data);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var resp = await _categoryService.DeleteAsync(id);
            if (!resp.Success)
            {
                TempData["ErrorMessage"] = resp.Message ?? resp.ErrorDetails;
            }
            else
            {
                TempData["SuccessMessage"] = "Thể loại đã được xóa thành công.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CheckNameExists(string name, Guid? excludeId = null)
        {
            var resp = await _categoryService.IsNameExistsAsync(name, excludeId);
            if (resp.Success)
            {
                return Json(new { exists = resp.Data });
            }
            return Json(new { exists = false });
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoriesWithProductCount()
        {
            var resp = await _categoryService.GetAllWithProductCountAsync();
            if (resp.Success)
            {
                return Json(new { success = true, data = resp.Data });
            }
            return Json(new { success = false, message = resp.Message });
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveCategories()
        {
            var resp = await _categoryService.GetActiveCategoriesAsync();
            if (resp.Success)
            {
                return Json(new { success = true, data = resp.Data });
            }
            return Json(new { success = false, message = resp.Message });
        }
    }
}
