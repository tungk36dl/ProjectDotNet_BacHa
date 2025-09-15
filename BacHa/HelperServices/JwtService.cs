using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BacHa.Models;

namespace BacHa.Services
{
    public class JwtService : IJwtService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IConfiguration config, ILogger<JwtService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public string GenerateToken(User user)
        {
            var jwt = _config.GetSection("Jwt");
            var keyValue = jwt["Key"] ?? throw new InvalidOperationException("JWT Key not found in configuration");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, user.Role?.Name ?? user.RoleName ?? "User")
            };

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiryMinutes"] ?? "60")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public bool ValidateRefreshToken(User user, string refreshToken)
        {
            if (user == null || string.IsNullOrEmpty(user.RefreshToken) || string.IsNullOrEmpty(refreshToken))
                return false;

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return false;

            return user.RefreshToken == refreshToken;
        }

        public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var jwt = _config.GetSection("Jwt");
            var keyValue = jwt["Key"] ?? throw new InvalidOperationException("JWT Key not found in configuration");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
            
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = true,
                ValidAudience = jwt["Audience"],
                ValidateIssuer = true,
                ValidIssuer = jwt["Issuer"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateLifetime = false // We don't care about the token expiry
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            try
            {
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
                if (securityToken is not JwtSecurityToken jwtSecurityToken || 
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                    throw new SecurityTokenException("Invalid token");

                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return null;
            }
        }

        public string GenerateTokenFromClaims(ClaimsPrincipal claims)
        {
            var jwt = _config.GetSection("Jwt");
            var keyValue = jwt["Key"] ?? throw new InvalidOperationException("JWT Key not found in configuration");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyValue));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwt["Issuer"],
                audience: jwt["Audience"],
                claims: claims.Claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(jwt["ExpiryMinutes"] ?? "60")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
