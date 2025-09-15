using Microsoft.AspNetCore.Mvc;
using BacHa.Models;
using BacHa.Models.Service;
using BacHa.Models.Service.RoleService;
using BacHa.Models.Service.RoleService.Dto;
using BacHa.Models.Service.RoleService.ViewModels;
using BacHa.Helper;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace BacHa.Controllers
{
    public class RolesController : BaseController
    {
        private readonly IRoleService _roleService;
        private readonly ILogger<RolesController> _logger;

        public RolesController(IRoleService roleService, ILogger<RolesController> logger)
        {
            _roleService = roleService;
            _logger = logger;
        }

        public async Task<IActionResult> Index([FromQuery] RoleSearch? search)
        {
            var resp = await _roleService.GetAllAsync(search);
            if (!resp.Success)
            {
                ViewBag.ErrorMessage = resp.Message ?? resp.ErrorDetails;
                ViewBag.Search = search ?? new RoleSearch();
                return View(new List<Role>());
            }
            ViewBag.Search = search ?? new RoleSearch();
            return View(resp.Data ?? new List<Role>());
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var resp = await _roleService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null) return NotFound();
            return View(resp.Data);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleCreateVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var role = new Role
            {
                Name = model.Name,
                Description = model.Description,
                IsActive = model.IsActive
            };

            var result = await _roleService.AddAsync(role);
            if (!result.Success)
            {
                ModelState.AddDataResponse(new DataResponse<object> { Success = result.Success, Message = result.Message, ErrorDetails = result.ErrorDetails });
                return View(model);
            }

            TempData["SuccessMessage"] = "Role created successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var resp = await _roleService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null) return NotFound();

            var model = new RoleUpdateVM
            {
                Id = resp.Data.Id,
                Name = resp.Data.Name,
                Description = resp.Data.Description,
                IsActive = resp.Data.IsActive
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, RoleUpdateVM model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var role = new Role
            {
                Id = model.Id,
                Name = model.Name,
                Description = model.Description,
                IsActive = model.IsActive
            };

            var resp = await _roleService.UpdateAsync(role);
            if (!resp.Success)
            {
                ModelState.AddDataResponse(new DataResponse<object> { Success = resp.Success, Message = resp.Message, ErrorDetails = resp.ErrorDetails });
                return View(model);
            }

            TempData["SuccessMessage"] = "Role updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var resp = await _roleService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null) return NotFound();
            return View(resp.Data);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var resp = await _roleService.DeleteAsync(id);
            if (!resp.Success)
            {
                TempData["ErrorMessage"] = resp.Message ?? resp.ErrorDetails;
            }
            else
            {
                TempData["SuccessMessage"] = "Role deleted successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CheckNameExists(string name, Guid? excludeId = null)
        {
            var resp = await _roleService.IsNameExistsAsync(name, excludeId);
            if (resp.Success)
            {
                return Json(new { exists = resp.Data });
            }
            return Json(new { exists = false });
        }

        [HttpGet]
        public async Task<IActionResult> GetRolesWithUserCount()
        {
            var resp = await _roleService.GetAllWithUserCountAsync();
            if (resp.Success)
            {
                return Json(new { success = true, data = resp.Data });
            }
            return Json(new { success = false, message = resp.Message });
        }
    }
}
