using BacHa.Models;
using BacHa.Models.Service;
using BacHa.Models.Service.UserService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace BacHa.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthApiController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthApiController(IUserService userService)
        {
            _userService = userService;
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { success = false, message = "Username and password are required" });
            }

            try
            {
                // Find user by username or email
                var usersResp = await _userService.GetAllAsync();
                var users = usersResp.Data ?? new List<User>();
                var user = users.Find(u => string.Equals(u.UserName, request.UsernameOrEmail, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(u.Email, request.UsernameOrEmail, StringComparison.OrdinalIgnoreCase));

                if (user == null)
                {
                    return Unauthorized(new { success = false, message = "Invalid credentials" });
                }

                var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? string.Empty, request.Password);
                if (verify == PasswordVerificationResult.Failed)
                {
                    return Unauthorized(new { success = false, message = "Invalid credentials" });
                }

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

                return Ok(new 
                { 
                    success = true, 
                    message = "Login successful",
                    user = new
                    {
                        id = user.Id,
                        username = user.UserName,
                        email = user.Email,
                        fullName = user.FullName,
                        role = user.Role?.Name ?? user.RoleName
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while logging in" });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });
            }

            try
            {
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = request.Username,
                    Email = request.Email,
                    FullName = request.FullName,
                    PasswordHash = _passwordHasher.HashPassword(new User(), request.Password),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var result = await _userService.AddAsync(user);
                
                if (!result.Success)
                {
                    return BadRequest(new { success = false, message = result.Message, errors = result.ErrorDetails });
                }

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

                return Ok(new 
                { 
                    success = true, 
                    message = "Registration successful",
                    user = new
                    {
                        id = user.Id,
                        username = user.UserName,
                        email = user.Email,
                        fullName = user.FullName
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "An error occurred while registering" });
            }
        }

        [HttpGet("validate-session")]
        public IActionResult ValidateSession()
        {
            // Check if user is authenticated via session
            if (User.Identity?.IsAuthenticated == true)
            {
                return Ok(new { 
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
            return Unauthorized(new { success = false, message = "Session is invalid" });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { success = true, message = "Logged out successfully" });
        }
    }

    // Request models
    public class LoginRequest
    {
        [Required]
        public string UsernameOrEmail { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string FullName { get; set; } = string.Empty;
        
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }

}
