using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BacHa.Models.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly MyDbContext _db;
        private bool _disposed;

        public UnitOfWork(MyDbContext db)
        {
            _db = db;
        }

        public IQueryable<User> Users => _db.Users.AsQueryable();

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _db.Users.AsNoTracking().OrderBy(u => u.UserName).ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _db.Users.FindAsync(id);
        }

        public async Task AddUserAsync(User user)
        {
            _db.Users.Add(user);
            await SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            _db.Users.Update(user);
            await SaveChangesAsync();
        }

        public async Task DeleteUserAsync(Guid id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user != null)
            {
                _db.Users.Remove(user);
                await SaveChangesAsync();
            }
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _db.SaveChangesAsync();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _db.Dispose();
                }
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
