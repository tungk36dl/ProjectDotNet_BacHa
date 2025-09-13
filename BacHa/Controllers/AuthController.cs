using BacHa.Models;
using BacHa.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using System;
using BacHa.Models.Service;

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
                    Response.Cookies.Append("X-Access-Token", token);
                    return Json(new { success = true });
                }
                return Json(new { success = false, message = op.Message, fieldErrors = op.FieldErrors });
            }

            if (op.Success)
            {
                    var token = _jwtService.GenerateToken(model);
                Response.Cookies.Append("X-Access-Token", token);
                return RedirectToAction("Index", "Home");
            }

            if (!string.IsNullOrWhiteSpace(op.Message)) ModelState.AddModelError(string.Empty, op.Message);
            foreach (var kv in op.FieldErrors)
            {
                foreach (var err in kv.Value)
                {
                    ModelState.AddModelError(kv.Key, err);
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
            var users = await _userService.GetAllAsync();
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
                // create token
                var token = _jwtService.GenerateToken(user);
                Response.Cookies.Append("X-Access-Token", token);

                if (isAjax) return Json(new { success = true });
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

        public IActionResult Logout()
        {
            Response.Cookies.Delete("X-Access-Token");
            return RedirectToAction("Index", "Home");
        }
    }
}
