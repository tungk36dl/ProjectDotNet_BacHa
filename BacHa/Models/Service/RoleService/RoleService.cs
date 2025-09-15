using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BacHa.Models.UnitOfWork;
using BacHa.Models.Service.RoleService.Dto;

namespace BacHa.Models.Service.RoleService
{
    public class RoleService : IRoleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Role, Guid> _roleRepository;
        private readonly ILogger<RoleService> _logger;

        public RoleService(IUnitOfWork unitOfWork, IGenericRepository<Role, Guid> roleRepository, ILogger<RoleService> logger)
        {
            _unitOfWork = unitOfWork;
            _roleRepository = roleRepository;
            _logger = logger;
        }

        public async Task<DataResponse<List<Role>>> GetAllAsync(RoleSearch? search = null)
        {
            try
            {
                IQueryable<Role> query = _roleRepository.FindAll();

                if (search != null)
                {
                    if (!string.IsNullOrWhiteSpace(search.Query))
                    {
                        var qstr = search.Query.Trim();
                        query = query.Where(r => r.Name.Contains(qstr) || 
                                               (r.Description != null && r.Description.Contains(qstr)));
                    }

                    if (search.IsActive.HasValue)
                        query = query.Where(r => r.IsActive == search.IsActive.Value);

                    // paging
                    var skip = (Math.Max(1, search.Page) - 1) * Math.Max(1, search.PageSize);
                    query = query.Skip(skip).Take(Math.Max(1, search.PageSize));
                }

                var data = await query.OrderBy(r => r.Name).ToListAsync();
                return new DataResponse<List<Role>> { Success = true, Data = data };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles");
                return new DataResponse<List<Role>> { Success = false, Message = "Failed to get roles.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<Role?>> GetByIdAsync(Guid id)
        {
            try
            {
                var role = await _roleRepository.FindByIdAsync(id);
                return new DataResponse<Role?> { Success = true, Data = role };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role by id: {RoleId}", id);
                return new DataResponse<Role?> { Success = false, Message = $"Failed to get role by id: {id}", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<Role>> AddAsync(Role role)
        {
            if (role == null)
            {
                return new DataResponse<Role> { Success = false, Message = "Role is required." };
            }

            try
            {
                // Validate data annotations
                var ctx = new ValidationContext(role);
                Validator.ValidateObject(role, ctx, validateAllProperties: true);

                // Check uniqueness of Name
                var fieldErrors = new Dictionary<string, List<string>>();
                if (!string.IsNullOrWhiteSpace(role.Name))
                {
                    var existsName = await _roleRepository.AnyAsync(r => r.Name == role.Name);
                    if (existsName)
                    {
                        fieldErrors.TryAdd(nameof(role.Name), new List<string>());
                        fieldErrors[nameof(role.Name)].Add("Role name already exists.");
                    }
                }

                if (fieldErrors.Any())
                {
                    return new DataResponse<Role>
                    {
                        Success = false,
                        Message = "Validation errors",
                        ErrorDetails = System.Text.Json.JsonSerializer.Serialize(fieldErrors)
                    };
                }

                if (role.Id == Guid.Empty) role.Id = Guid.NewGuid();
                role.CreatedAt = DateTime.UtcNow;
                role.UpdatedAt = DateTime.UtcNow;

                _roleRepository.Add(role);
                await _unitOfWork.SaveChangesAsync();
                return new DataResponse<Role> { Success = true, Data = role };
            }
            catch (ValidationException vex)
            {
                return new DataResponse<Role> { Success = false, Message = vex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding role");
                return new DataResponse<Role> { Success = false, Message = "Failed to add role.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<Role>> UpdateAsync(Role role)
        {
            if (role == null)
            {
                return new DataResponse<Role> { Success = false, Message = "Role is required." };
            }

            try
            {
                var ctx = new ValidationContext(role);
                Validator.ValidateObject(role, ctx, validateAllProperties: true);

                var fieldErrors = new Dictionary<string, List<string>>();
                // Check uniqueness excluding current role
                if (!string.IsNullOrWhiteSpace(role.Name))
                {
                    var existsName = await _roleRepository.AnyAsync(r => r.Id != role.Id && r.Name == role.Name);
                    if (existsName)
                    {
                        fieldErrors.TryAdd(nameof(role.Name), new List<string>());
                        fieldErrors[nameof(role.Name)].Add("Role name already exists.");
                    }
                }

                if (fieldErrors.Any())
                {
                    return new DataResponse<Role>
                    {
                        Success = false,
                        Message = "Validation errors",
                        ErrorDetails = System.Text.Json.JsonSerializer.Serialize(fieldErrors)
                    };
                }

                role.UpdatedAt = DateTime.UtcNow;
                _roleRepository.Update(role);
                await _unitOfWork.SaveChangesAsync();
                return new DataResponse<Role> { Success = true, Data = role };
            }
            catch (ValidationException vex)
            {
                return new DataResponse<Role> { Success = false, Message = vex.Message };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role");
                return new DataResponse<Role> { Success = false, Message = "Failed to update role.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<object>> DeleteAsync(Guid id)
        {
            try
            {
                var role = await _roleRepository.FindByIdAsync(id);
                if (role == null)
                {
                    return new DataResponse<object> { Success = false, Message = "Role not found." };
                }

                // Check if role is being used by any users
                var userCount = await _roleRepository.FindAll(r => r.Id == id)
                    .SelectMany(r => r.Users)
                    .CountAsync();

                if (userCount > 0)
                {
                    return new DataResponse<object> 
                    { 
                        Success = false, 
                        Message = $"Cannot delete role. It is being used by {userCount} user(s)." 
                    };
                }

                _roleRepository.Remove(role);
                await _unitOfWork.SaveChangesAsync();
                return new DataResponse<object> { Success = true, Data = null };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting role: {RoleId}", id);
                return new DataResponse<object> { Success = false, Message = $"Failed to delete role: {id}", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<List<RoleDto>>> GetAllWithUserCountAsync()
        {
            try
            {
                var roles = await _roleRepository.FindAll()
                    .Select(r => new RoleDto
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Description = r.Description,
                        IsActive = r.IsActive,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        UserCount = r.Users.Count
                    })
                    .OrderBy(r => r.Name)
                    .ToListAsync();

                return new DataResponse<List<RoleDto>> { Success = true, Data = roles };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles with user count");
                return new DataResponse<List<RoleDto>> { Success = false, Message = "Failed to get roles.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<bool>> IsNameExistsAsync(string name, Guid? excludeId = null)
        {
            try
            {
                var exists = await _roleRepository.AnyAsync(r => r.Name == name && (excludeId == null || r.Id != excludeId));
                return new DataResponse<bool> { Success = true, Data = exists };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking role name existence");
                return new DataResponse<bool> { Success = false, Message = "Failed to check role name.", ErrorDetails = ex.Message };
            }
        }
    }
}

