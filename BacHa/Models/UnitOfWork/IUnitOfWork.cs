using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BacHa.Models.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IQueryable<User> Users { get; }

        Task<List<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(Guid id);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task DeleteUserAsync(Guid id);

        Task<int> SaveChangesAsync();
    }
}
