using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using BacHa.Models;
using BacHa.Models.Service;
using BacHa.Helper;

namespace BacHa.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public abstract class BaseController : Controller
    {
        protected User? CurrentUser
        {
            get
            {
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        return new User
                        {
                            Id = userId,
                            UserName = User.FindFirst(ClaimTypes.Name)?.Value,
                            Email = User.FindFirst(ClaimTypes.Email)?.Value,
                            RoleName = User.FindFirst(ClaimTypes.Role)?.Value
                        };
                    }
                }
                return null;
            }
        }

        protected bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

        protected string? UserRole => User.FindFirst(ClaimTypes.Role)?.Value;

        protected Guid? UserId
        {
            get
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                return userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId) ? userId : null;
            }
        }

        /// <summary>
        /// Handle service response and return appropriate view or redirect
        /// </summary>
        protected IActionResult HandleServiceResponse<T>(DataResponse<T> response, string successAction = "Index", object? routeValues = null)
        {
            if (response.Success)
            {
                if (response.Data != null)
                    return View(response.Data);
                return RedirectToAction(successAction, routeValues);
            }

            ModelState.AddDataResponse(new DataResponse<object> 
            { 
                Success = response.Success, 
                Message = response.Message, 
                ErrorDetails = response.ErrorDetails 
            });
            return View();
        }

        /// <summary>
        /// Handle service response for API calls
        /// </summary>
        protected IActionResult HandleApiResponse<T>(DataResponse<T> response)
        {
            if (response.Success)
            {
                return Json(new { success = true, data = response.Data });
            }

            return Json(new { 
                success = false, 
                message = response.Message,
                errorDetails = response.ErrorDetails
            });
        }
    }
}
