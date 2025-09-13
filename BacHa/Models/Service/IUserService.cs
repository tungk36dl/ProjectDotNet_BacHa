using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BacHa.Models.Service
{
    public interface IUserService
    {
        Task<List<User>> GetAllAsync();
        Task<User?> GetByIdAsync(Guid id);
    Task<OperationResult> AddAsync(User user);
    Task<OperationResult> UpdateAsync(User user);
        Task DeleteAsync(Guid id);
    }
}
e