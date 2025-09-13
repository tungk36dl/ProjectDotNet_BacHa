using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BacHa.Models.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BacHa.Models.Service
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _uow;

        public UserService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<List<User>> GetAllAsync()
        {
            try
            {
                return await _uow.GetAllUsersAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to get users.", ex);
            }
        }

        public async Task<User?> GetByIdAsync(Guid id)
        {
            try
            {
                return await _uow.GetUserByIdAsync(id);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to get user by id: {id}", ex);
            }
        }

        public async Task<OperationResult> AddAsync(User user)
        {
            var result = new OperationResult();
            if (user == null)
            {
                result.Success = false;
                result.Message = "User is required.";
                return result;
            }

            try
            {
                // Validate data annotations on User
                var ctx = new ValidationContext(user);
                Validator.ValidateObject(user, ctx, validateAllProperties: true);

                // Check uniqueness of UserName and Email
                if (!string.IsNullOrWhiteSpace(user.UserName))
                {
                    var existsUserName = await _uow.Users.AnyAsync(u => u.UserName == user.UserName);
                    if (existsUserName)
                    {
                        result.AddError(nameof(user.UserName), "UserName already exists.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    var existsEmail = await _uow.Users.AnyAsync(u => u.Email == user.Email);
                    if (existsEmail)
                    {
                        result.AddError(nameof(user.Email), "Email already exists.");
                    }
                }

                if (!result.Success) return result;

                if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();
                user.CreatedAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                await _uow.AddUserAsync(user);
                result.Success = true;
                return result;
            }
            catch (ValidationException vex)
            {
                var r = new OperationResult();
                r.Success = false;
                r.Message = vex.Message;
                return r;
            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.Success = false;
                r.Message = "Failed to add user.";
                return r;
            }
        }

        public async Task<OperationResult> UpdateAsync(User user)
        {
            var result = new OperationResult();
            if (user == null)
            {
                result.Success = false;
                result.Message = "User is required.";
                return result;
            }

            try
            {
                var ctx = new ValidationContext(user);
                Validator.ValidateObject(user, ctx, validateAllProperties: true);

                // uniqueness excluding current user
                if (!string.IsNullOrWhiteSpace(user.UserName))
                {
                    var existsUserName = await _uow.Users.AnyAsync(u => u.Id != user.Id && u.UserName == user.UserName);
                    if (existsUserName)
                        result.AddError(nameof(user.UserName), "UserName already exists.");
                }

                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    var existsEmail = await _uow.Users.AnyAsync(u => u.Id != user.Id && u.Email == user.Email);
                    if (existsEmail)
                        result.AddError(nameof(user.Email), "Email already exists.");
                }

                if (!result.Success) return result;

                user.UpdatedAt = DateTime.UtcNow;
                await _uow.UpdateUserAsync(user);
                result.Success = true;
                return result;
            }
            catch (ValidationException vex)
            {
                var r = new OperationResult();
                r.Success = false;
                r.Message = vex.Message;
                return r;
            }
            catch (Exception ex)
            {
                var r = new OperationResult();
                r.Success = false;
                r.Message = "Failed to update user.";
                return r;
            }
        }

        public async Task DeleteAsync(Guid id)
        {
            try
            {
                await _uow.DeleteUserAsync(id);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Failed to delete user: {id}", ex);
            }
        }
    }
}
