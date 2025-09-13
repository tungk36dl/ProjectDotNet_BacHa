using System;
using BacHa.Models;

namespace BacHa.Services
{
    public interface IJwtService
    {
        string GenerateToken(User user);
    }
}
