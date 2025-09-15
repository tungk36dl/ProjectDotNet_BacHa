using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BacHa.Services;

namespace BacHa.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthenticationMiddleware> _logger;

        public AuthenticationMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<AuthenticationMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
        {
            // Get token from Authorization header first, then from cookies
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
            var token = authHeader?.StartsWith("Bearer ") == true ? authHeader.Substring("Bearer ".Length).Trim() : null;
            
            if (string.IsNullOrEmpty(token))
            {
                token = context.Request.Cookies["X-Access-Token"];
            }
            
            var refreshToken = context.Request.Cookies["X-Refresh-Token"];

            // Skip authentication for auth endpoints and static files
            if (ShouldSkipAuthentication(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // If no token, redirect to login
            if (string.IsNullOrEmpty(token))
            {
                await HandleUnauthorized(context);
                return;
            }

            // Validate token
            if (!ValidateToken(token, out ClaimsPrincipal? principal))
            {
                // Try to refresh token
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    var newToken = await TryRefreshToken(context, refreshToken, jwtService);
                    if (!string.IsNullOrEmpty(newToken))
                    {
                        // Set new token and continue
                        context.Request.Headers["Authorization"] = $"Bearer {newToken}";
                        await _next(context);
                        return;
                    }
                }

                await HandleUnauthorized(context);
                return;
            }

            // Set user context
            context.User = principal;
            await _next(context);
        }

        private bool ShouldSkipAuthentication(PathString path)
        {
            var skipPaths = new[]
            {
                "/Auth/Login",
                "/Auth/Register",
                "/Auth/RefreshToken",
                "/Home/Index",
                "/Home/Privacy",
                "/css/",
                "/js/",
                "/lib/",
                "/favicon.ico"
            };

            return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath));
        }

        private bool ValidateToken(string token, out ClaimsPrincipal? principal)
        {
            principal = null;
            try
            {
                var jwt = _configuration.GetSection("Jwt");
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]));
                
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidAudience = jwt["Audience"],
                    ValidateIssuer = true,
                    ValidIssuer = jwt["Issuer"],
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return false;
            }
        }

        private async Task<string?> TryRefreshToken(HttpContext context, string refreshToken, IJwtService jwtService)
        {
            try
            {
                // This is a simplified version - in production, you'd want to use a proper service
                // to find the user by refresh token
                var response = await context.RequestServices.GetRequiredService<BacHa.Models.Service.IUserService>()
                    .GetAllAsync();
                
                if (response.Success && response.Data != null)
                {
                    var user = response.Data.FirstOrDefault(u => u.RefreshToken == refreshToken);
                    if (user != null && jwtService.ValidateRefreshToken(user, refreshToken))
                    {
                        // Generate new tokens
                        var newToken = jwtService.GenerateToken(user);
                        var newRefreshToken = jwtService.GenerateRefreshToken();

                        // Update user with new refresh token
                        user.RefreshToken = newRefreshToken;
                        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                        await context.RequestServices.GetRequiredService<BacHa.Models.Service.IUserService>()
                            .UpdateAsync(user);

                        // Set new cookies
                        context.Response.Cookies.Append("X-Access-Token", newToken);
                        context.Response.Cookies.Append("X-Refresh-Token", newRefreshToken);

                        return newToken;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
            }

            return null;
        }

        private async Task HandleUnauthorized(HttpContext context)
        {
            // Clear invalid tokens
            context.Response.Cookies.Delete("X-Access-Token");
            context.Response.Cookies.Delete("X-Refresh-Token");

            // For AJAX requests, return 401
            if (context.Request.Headers.ContainsKey("X-Requested-With") && 
                context.Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized");
                return;
            }

            // For regular requests, redirect to login
            context.Response.Redirect("/Auth/Login");
        }
    }

    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthenticationMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }
}
