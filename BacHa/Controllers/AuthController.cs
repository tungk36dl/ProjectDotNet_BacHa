using BacHa.Models;
using BacHa.Services;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System;
using BacHa.Models.Service;
using BacHa.Models.Service.UserService;

namespace BacHa.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthController(IUserService userService, IJwtService jwtService)
        {
            _userService = userService;
            _jwtService = jwtService;
            _passwordHasher = new PasswordHasher<User>();
        }

        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(User model, string password)
        {
            if (!ModelState.IsValid) return View(model);
            if (string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Password", "Password is required");
                return View(model);
            }

            model.Id = Guid.NewGuid();
            model.PasswordHash = _passwordHasher.HashPassword(model, password);

            var op = await _userService.AddAsync(model);

            var isAjax = Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            if (isAjax)
            {
                if (op.Success)
                {
                    var token = _jwtService.GenerateToken(model);
                    var refreshToken = _jwtService.GenerateRefreshToken();
                    
                    // Update user with refresh token
                    model.RefreshToken = refreshToken;
                    model.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                    await _userService.UpdateAsync(model);
                    
                    // Set cookies
                    Response.Cookies.Append("X-Access-Token", token);
                    Response.Cookies.Append("X-Refresh-Token", refreshToken);
                    
                    return Json(new { success = true, token = token, refreshToken = refreshToken });
                }

                // try parse field errors from ErrorDetails
                object? fieldErrors = null;
                if (!string.IsNullOrEmpty(op.ErrorDetails))
                {
                    try
                    {
                        fieldErrors = System.Text.Json.JsonSerializer.Deserialize<object>(op.ErrorDetails);
                    }
                    catch { }
                }

                return Json(new { success = false, message = op.Message, fieldErrors });
            }

            if (op.Success)
            {
                var token = _jwtService.GenerateToken(model);
                var refreshToken = _jwtService.GenerateRefreshToken();
                
                // Update user with refresh token
                model.RefreshToken = refreshToken;
                model.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _userService.UpdateAsync(model);
                
                // Set cookies
                Response.Cookies.Append("X-Access-Token", token);
                Response.Cookies.Append("X-Refresh-Token", refreshToken);
                
                return RedirectToAction("Index", "Home");
            }

            if (!string.IsNullOrWhiteSpace(op.Message)) ModelState.AddModelError(string.Empty, op.Message);

            if (!string.IsNullOrEmpty(op.ErrorDetails))
            {
                try
                {
                    var dict = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, List<string>>>(op.ErrorDetails!);
                    if (dict != null)
                    {
                        foreach (var kv in dict)
                        {
                            foreach (var err in kv.Value)
                            {
                                ModelState.AddModelError(kv.Key, err);
                            }
                        }
                    }
                }
                catch
                {
                    ModelState.AddModelError(string.Empty, op.ErrorDetails);
                }
            }

            return View(model);
        }

        public IActionResult Login() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string usernameOrEmail, string password)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError(string.Empty, "Username and password required");
                return View();
            }

            // find user by username or email
            var usersResp = await _userService.GetAllAsync();
            var users = usersResp.Data ?? new List<User>();
            var user = users.Find(u => string.Equals(u.UserName, usernameOrEmail, StringComparison.OrdinalIgnoreCase)
                || string.Equals(u.Email, usernameOrEmail, StringComparison.OrdinalIgnoreCase));

            var isAjax = Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (user == null)
            {
                if (isAjax) return Json(new { success = false, message = "Invalid credentials" });
                ModelState.AddModelError(string.Empty, "Invalid credentials");
                return View();
            }

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, password);
            if (verify == PasswordVerificationResult.Failed)
            {
                if (isAjax) return Json(new { success = false, message = "Invalid credentials" });
                ModelState.AddModelError(string.Empty, "Invalid credentials");
                return View();
            }

            try
            {
                // Generate tokens
                var token = _jwtService.GenerateToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();
                
                // Update user with refresh token
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // 7 days expiry
                await _userService.UpdateAsync(user);

                // Set cookies
                Response.Cookies.Append("X-Access-Token", token);
                Response.Cookies.Append("X-Refresh-Token", refreshToken);

                if (isAjax) return Json(new { success = true, token = token, refreshToken = refreshToken });
                return RedirectToAction("Index", "Home");
            }
            catch (Exception)
            {
                if (isAjax) return Json(new { success = false, message = "An error occurred while logging in. Please try again." });
                ModelState.AddModelError(string.Empty, "An error occurred while logging in. Please try again.");
                // optionally log exception
                return View();
            }
        }

        [HttpGet]
        public IActionResult ValidateToken()
        {
            // This method is used by the frontend to check if the current token is valid
            // The middleware will handle the actual validation
            return Json(new { success = true, message = "Token is valid" });
        }

        [HttpPost]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["X-Refresh-Token"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return Json(new { success = false, message = "Refresh token not found" });
            }

            // Find user by refresh token
            var usersResp = await _userService.GetAllAsync();
            var users = usersResp.Data ?? new List<User>();
            var user = users.FirstOrDefault(u => u.RefreshToken == refreshToken);

            if (user == null || !_jwtService.ValidateRefreshToken(user, refreshToken))
            {
                return Json(new { success = false, message = "Invalid refresh token" });
            }

            try
            {
                // Generate new tokens
                var newToken = _jwtService.GenerateToken(user);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                // Update user with new refresh token
                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await _userService.UpdateAsync(user);

                // Set new cookies
                Response.Cookies.Append("X-Access-Token", newToken);
                Response.Cookies.Append("X-Refresh-Token", newRefreshToken);

                return Json(new { success = true, token = newToken, refreshToken = newRefreshToken });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error refreshing token" });
            }
        }

        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["X-Refresh-Token"];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                // Find user and clear refresh token
                var usersResp = await _userService.GetAllAsync();
                var users = usersResp.Data ?? new List<User>();
                var user = users.FirstOrDefault(u => u.RefreshToken == refreshToken);
                
                if (user != null)
                {
                    user.RefreshToken = null;
                    user.RefreshTokenExpiryTime = null;
                    await _userService.UpdateAsync(user);
                }
            }

            Response.Cookies.Delete("X-Access-Token");
            Response.Cookies.Delete("X-Refresh-Token");
            return RedirectToAction("Index", "Home");
        }
    }
}
