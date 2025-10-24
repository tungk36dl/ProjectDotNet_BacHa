using Microsoft.AspNetCore.Mvc;
using BacHa.Models;
using BacHa.Models.Service;
using BacHa.Models.Service.RoleService;
using BacHa.Models.Service.UserService.ViewModels;
using BacHa.Helper;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace BacHa.Controllers
{
    [Authorize]
    public class UsersController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly PasswordHasher<User> _passwordHasher;

        public UsersController(IUserService userService, IRoleService roleService)
        {
            _userService = userService;
            _roleService = roleService;
            _passwordHasher = new PasswordHasher<User>();
        }
        public async Task<IActionResult> Index([FromQuery] Models.Service.UserService.Dto.UserSearch? search)
        {
            var resp = await _userService.GetAllAsync(search);
            if (!resp.Success)
            {
                ViewBag.ErrorMessage = resp.Message ?? resp.ErrorDetails;
                ViewBag.Search = search ?? new Models.Service.UserService.Dto.UserSearch();
                return View(new List<User>());
            }
            ViewBag.Search = search ?? new Models.Service.UserService.Dto.UserSearch();
            return View(resp.Data ?? new List<User>());
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var resp = await _userService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null) return NotFound();
            return View(resp.Data);
        }
        public async Task<IActionResult> Create()
        {
            await LoadRolesAsync();
            return View(new UserCreateVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateVM model)
        {
            if (!ModelState.IsValid)
            {
                await LoadRolesAsync();
                return View(model);
            }

            // Convert ViewModel to User entity
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = model.UserName,
                Email = model.Email,
                FullName = model.FullName,
                IsActive = model.IsActive,
                RoleName = model.Role,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Hash password
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
            }

            var result = await _userService.AddAsync(user);
            if (!result.Success)
            {
                ModelState.AddDataResponse(new DataResponse<object> { Success = result.Success, Message = result.Message, ErrorDetails = result.ErrorDetails });
                await LoadRolesAsync();
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var resp = await _userService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null) return NotFound();
            await LoadRolesAsync();
            return View(resp.Data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, User user)
        {
            if (id != user.Id) return BadRequest();
            if (!ModelState.IsValid) return View(user);

            var resp = await _userService.UpdateAsync(user);
            if (!resp.Success)
            {
                ModelState.AddDataResponse(new DataResponse<object> { Success = resp.Success, Message = resp.Message, ErrorDetails = resp.ErrorDetails });
                return View(user);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var resp = await _userService.GetByIdAsync(id);
            if (!resp.Success || resp.Data == null) return NotFound();
            return View(resp.Data);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var resp = await _userService.DeleteAsync(id);
            if (!resp.Success)
            {
                TempData["ErrorMessage"] = resp.Message ?? resp.ErrorDetails;
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadRolesAsync()
        {
            var rolesResp = await _roleService.GetAllAsync();
            if (rolesResp.Success && rolesResp.Data != null)
            {
                ViewBag.Roles = new SelectList(rolesResp.Data.Where(r => r.IsActive), "Name", "Name");
            }
            else
            {
                ViewBag.Roles = new SelectList(new List<Role>(), "Name", "Name");
            }
        }
    }
}
