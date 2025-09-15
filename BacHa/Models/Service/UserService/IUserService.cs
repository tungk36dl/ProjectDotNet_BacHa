using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BacHa.Models.Service
{
    public interface IUserService
    {
    Task<DataResponse<List<User>>> GetAllAsync(UserService.Dto.UserSearch? search = null);
        Task<DataResponse<User?>> GetByIdAsync(Guid id);
        Task<DataResponse<User>> AddAsync(User user);
        Task<DataResponse<User>> UpdateAsync(User user);
        Task<DataResponse<object>> DeleteAsync(Guid id);
    }
}
