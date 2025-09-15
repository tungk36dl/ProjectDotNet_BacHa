using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BacHa.Models;
using BacHa.Models.UnitOfWork;

namespace BacHa.HelperServices
{
    public class DataSeedService : IDataSeedService
    {
        private readonly ApplicationDBContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly PasswordHasher<User> _passwordHasher;

        public DataSeedService(ApplicationDBContext context, IUnitOfWork unitOfWork)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task SeedDefaultAdminUserAsync()
        {
            // Check if admin user already exists
            var existingAdmin = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == "admin");

            if (existingAdmin != null)
                return;

            // Get admin role
            var adminRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "Admin");

            if (adminRole == null)
                return;

            // Create admin user
            var adminUser = new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                UserName = "admin",
                Email = "admin@bacha.com",
                FullName = "System Administrator",
                IsActive = true,
                RoleId = adminRole.Id,
                RoleName = "Admin",
                CreatedAt = DateTime.UtcNow
            };

            // Hash password
            adminUser.PasswordHash = _passwordHasher.HashPassword(adminUser, "123");

            // Add to database
            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();
        }
    }
}
