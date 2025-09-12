using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BacHa.Models.UnitOfWork;

namespace BacHa.Models.Service
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;

        public UserService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public Task<List<User>> GetAllAsync() => _uow.GetAllUsersAsync();

        public Task<User?> GetByIdAsync(Guid id) => _uow.GetUserByIdAsync(id);

        public Task AddAsync(User user)
        {
            if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();
            return _uow.AddUserAsync(user);
        }

        public Task UpdateAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            return _uow.UpdateUserAsync(user);
        }

        public Task DeleteAsync(Guid id) => _uow.DeleteUserAsync(id);
    }
}
