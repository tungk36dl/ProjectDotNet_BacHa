using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using BacHa.Models.UnitOfWork;
using BacHa.Models.Service.UserService.Dto;

namespace BacHa.Models.Service.UserService
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<User, Guid> _userRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(IUnitOfWork unitOfWork, IGenericRepository<User, Guid> userRepository, ILogger<UserService> logger)
        {
            _unitOfWork = unitOfWork;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task<DataResponse<List<User>>> GetAllAsync(UserSearch? search = null)
        {
            try
            {
                IQueryable<User> query = _userRepository.FindAll(null, r => r.Role);
                
                if (search != null)
                {
                    if (!string.IsNullOrWhiteSpace(search.Query))
                    {
                        var qstr = search.Query.Trim();
                        query = query.Where(u => (u.UserName != null && u.UserName.Contains(qstr))
                                                || (u.Email != null && u.Email.Contains(qstr))
                                                || (u.FullName != null && u.FullName.Contains(qstr)));
                    }

                    if (search.IsActive.HasValue)
                        query = query.Where(u => u.IsActive == search.IsActive.Value);

                    // paging
                    var skip = (Math.Max(1, search.Page) - 1) * Math.Max(1, search.PageSize);
                    query = query.Skip(skip).Take(Math.Max(1, search.PageSize));
                }

                var data = await query.ToListAsync();
                return new DataResponse<List<User>> { Success = true, Data = data };
            }
            catch (Exception ex)
            {
                return new DataResponse<List<User>> { Success = false, Message = "Failed to get users.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<User?>> GetByIdAsync(Guid id)
        {
            try
            {
                var u = await _userRepository.FindByIdAsync(id, r => r.Role);
                return new DataResponse<User?> { Success = true, Data = u };
            }
            catch (Exception ex)
            {
                return new DataResponse<User?> { Success = false, Message = $"Failed to get user by id: {id}", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<User>> AddAsync(User user)
        {
            if (user == null)
            {
                return new DataResponse<User> { Success = false, Message = "User is required." };
            }

            try
            {
                // Validate data annotations on User
                var ctx = new ValidationContext(user);
                Validator.ValidateObject(user, ctx, validateAllProperties: true);

                // Check uniqueness of UserName and Email
                var fieldErrors = new Dictionary<string, List<string>>();
                if (!string.IsNullOrWhiteSpace(user.UserName))
                {
                    var existsUserName = await _userRepository.AnyAsync(u => u.UserName == user.UserName);
                    if (existsUserName)
                    {
                        fieldErrors.TryAdd(nameof(user.UserName), new List<string>());
                        fieldErrors[nameof(user.UserName)].Add("UserName already exists.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    var existsEmail = await _userRepository.AnyAsync(u => u.Email == user.Email);
                    if (existsEmail)
                    {
                        fieldErrors.TryAdd(nameof(user.Email), new List<string>());
                        fieldErrors[nameof(user.Email)].Add("Email already exists.");
                    }
                }

                if (fieldErrors.Any())
                {
                    return new DataResponse<User>
                    {
                        Success = false,
                        Message = "Validation errors",
                        ErrorDetails = System.Text.Json.JsonSerializer.Serialize(fieldErrors)
                    };
                }

                if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                _userRepository.Add(user);
                await _unitOfWork.SaveChangesAsync();
                return new DataResponse<User> { Success = true, Data = user };
            }
            catch (ValidationException vex)
            {
                return new DataResponse<User> { Success = false, Message = vex.Message };
            }
            catch (Exception ex)
            {
                return new DataResponse<User> { Success = false, Message = "Failed to add user.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<User>> UpdateAsync(User user)
        {
            if (user == null)
            {
                return new DataResponse<User> { Success = false, Message = "User is required." };
            }

            try
            {
                var ctx = new ValidationContext(user);
                Validator.ValidateObject(user, ctx, validateAllProperties: true);

                var fieldErrors = new Dictionary<string, List<string>>();
                // uniqueness excluding current user
                if (!string.IsNullOrWhiteSpace(user.UserName))
                {
                    var existsUserName = await _userRepository.AnyAsync(u => u.Id != user.Id && u.UserName == user.UserName);
                    if (existsUserName)
                    {
                        fieldErrors.TryAdd(nameof(user.UserName), new List<string>());
                        fieldErrors[nameof(user.UserName)].Add("UserName already exists.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    var existsEmail = await _userRepository.AnyAsync(u => u.Id != user.Id && u.Email == user.Email);
                    if (existsEmail)
                    {
                        fieldErrors.TryAdd(nameof(user.Email), new List<string>());
                        fieldErrors[nameof(user.Email)].Add("Email already exists.");
                    }
                }

                if (fieldErrors.Any())
                {
                    return new DataResponse<User>
                    {
                        Success = false,
                        Message = "Validation errors",
                        ErrorDetails = System.Text.Json.JsonSerializer.Serialize(fieldErrors)
                    };
                }

                user.UpdatedAt = DateTime.UtcNow;
                _userRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();
                return new DataResponse<User> { Success = true, Data = user };
            }
            catch (ValidationException vex)
            {
                return new DataResponse<User> { Success = false, Message = vex.Message };
            }
            catch (Exception ex)
            {
                return new DataResponse<User> { Success = false, Message = "Failed to update user.", ErrorDetails = ex.Message };
            }
        }

        public async Task<DataResponse<object>> DeleteAsync(Guid id)
        {
            try
            {
                var user = await _userRepository.FindByIdAsync(id);
                if (user == null)
                {
                    return new DataResponse<object> { Success = false, Message = "User not found." };
                }

                _userRepository.Remove(user);
                await _unitOfWork.SaveChangesAsync();
                return new DataResponse<object> { Success = true, Data = null };
            }
            catch (Exception ex)
            {
                return new DataResponse<object> { Success = false, Message = $"Failed to delete user: {id}", ErrorDetails = ex.Message };
            }
        }

     
    }
}
