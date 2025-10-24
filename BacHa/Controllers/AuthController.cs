using BacHa.Models;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System;
using System.Linq;
using BacHa.Models.Service;
using BacHa.Models.Service.UserService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace BacHa.Controllers
{
    public class AuthController : Controller
    {
        private readonly IUserService _userService;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthController(IUserService userService)
        {
            _userService = userService;
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
                    // Create authentication claims
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, model.Id.ToString()),
                        new Claim(ClaimTypes.Name, model.UserName ?? string.Empty),
                        new Claim(ClaimTypes.Email, model.Email ?? string.Empty),
                        new Claim(ClaimTypes.Role, model.Role?.Name ?? model.RoleName ?? "User")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = true,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                        new ClaimsPrincipal(claimsIdentity), authProperties);
                    
                    return Json(new { success = true, message = "Registration successful" });
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
                // Create authentication claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, model.Id.ToString()),
                    new Claim(ClaimTypes.Name, model.UserName ?? string.Empty),
                    new Claim(ClaimTypes.Email, model.Email ?? string.Empty),
                    new Claim(ClaimTypes.Role, model.Role?.Name ?? model.RoleName ?? "User")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(claimsIdentity), authProperties);
                
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
        // [ValidateAntiForgeryToken] // Temporarily disabled for debugging
        public async Task<IActionResult> Login(string usernameOrEmail, string password)
        {
            Console.WriteLine($"Login attempt - UsernameOrEmail: '{usernameOrEmail}', Password: '{password}'");
            
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("Username or password is empty");
                ModelState.AddModelError(string.Empty, "Username and password required");
                if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Username and password required" });
                }
                return View();
            }

            // find user by username or email
            Console.WriteLine("Getting all users from database...");
            var usersResp = await _userService.GetAllAsync();
            var users = usersResp.Data ?? new List<User>();
            Console.WriteLine($"Found {users.Count} users in database");
            
            foreach (var u in users)
            {
                Console.WriteLine($"User: {u.UserName}, Email: {u.Email}, IsActive: {u.IsActive}");
            }
            
            var user = users.Find(u => string.Equals(u.UserName, usernameOrEmail, StringComparison.OrdinalIgnoreCase)
                || string.Equals(u.Email, usernameOrEmail, StringComparison.OrdinalIgnoreCase));

            var isAjax = Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest";
            Console.WriteLine($"Login request - isAjax: {isAjax}");
            Console.WriteLine($"Request headers: {string.Join(", ", Request.Headers.Select(h => $"{h.Key}={h.Value}"))}");

            if (user == null)
            {
                Console.WriteLine($"User not found for username/email: {usernameOrEmail}");
                if (isAjax) return Json(new { success = false, message = "Invalid credentials" });
                ModelState.AddModelError(string.Empty, "Invalid credentials");
                return View();
            }
            
            Console.WriteLine($"User found: {user.UserName}, IsActive: {user.IsActive}");

            var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, password);
            if (verify == PasswordVerificationResult.Failed)
            {
                if (isAjax) return Json(new { success = false, message = "Invalid credentials" });
                ModelState.AddModelError(string.Empty, "Invalid credentials");
                return View();
            }

            try
            {
                // Create authentication claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim(ClaimTypes.Role, user.Role?.Name ?? user.RoleName ?? "User")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                if (isAjax) 
                {
                    Console.WriteLine($"AJAX Login successful for user: {user.UserName}");
                    return Json(new { success = true, message = "Login successful" });
                }
                var userRole = user.Role?.Name ?? user.RoleName ?? "User";
                if (userRole == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }
                else if (userRole == "NhanVien")
                {
                    return RedirectToAction("Index", "NhanVien");
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
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
        public IActionResult ValidateSession()
        {
            // Check if user is authenticated via session
            if (User.Identity?.IsAuthenticated == true)
            {
                return Json(new { 
                    success = true, 
                    message = "Session is valid",
                    user = new {
                        id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                        name = User.FindFirst(ClaimTypes.Name)?.Value,
                        email = User.FindFirst(ClaimTypes.Email)?.Value,
                        role = User.FindFirst(ClaimTypes.Role)?.Value
                    }
                });
            }
            return Json(new { success = false, message = "Session is invalid" });
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
