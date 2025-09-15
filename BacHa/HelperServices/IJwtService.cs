using System;
using System.Security.Claims;
using BacHa.Models;

namespace BacHa.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
        string GenerateRefreshToken();
        bool ValidateRefreshToken(User user, string refreshToken);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
        string GenerateTokenFromClaims(ClaimsPrincipal claims);
    }
}
