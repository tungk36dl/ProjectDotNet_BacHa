using BacHa.Models.Service.RoleService.Dto;
using BacHa.Models;

namespace BacHa.Models.Service.RoleService
{
    public interface IRoleService
    {
        Task<DataResponse<List<Role>>> GetAllAsync(RoleSearch? search = null);
        Task<DataResponse<Role?>> GetByIdAsync(Guid id);
        Task<DataResponse<Role>> AddAsync(Role role);
        Task<DataResponse<Role>> UpdateAsync(Role role);
        Task<DataResponse<object>> DeleteAsync(Guid id);
        Task<DataResponse<List<RoleDto>>> GetAllWithUserCountAsync();
        Task<DataResponse<bool>> IsNameExistsAsync(string name, Guid? excludeId = null);
    }
}
