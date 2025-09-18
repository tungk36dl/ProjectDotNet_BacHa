using Microsoft.AspNetCore.Mvc;
using BacHa.Models;
using BacHa.Models.Service;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace BacHa.Controllers
{
    [Authorize]
    public class ProductsController : BaseController
    {
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ILogger<ProductsController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // This action requires authentication due to BaseController
            ViewBag.Message = $"Welcome {CurrentUser?.UserName ?? "User"}!";
            ViewBag.UserRole = UserRole;
            return View();
        }

        public IActionResult Create()
        {
            // Only authenticated users can access this
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(object model)
        {
            // Example of using BaseController's helper methods
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Simulate success response
            var response = new DataResponse<object>
            {
                Success = true,
                Message = "Product created successfully"
            };

            return HandleServiceResponse(response, "Index");
        }

        public IActionResult Details(Guid id)
        {
            // Example of checking user permissions
            if (UserRole != "Admin" && UserRole != "Manager")
            {
                TempData["ErrorMessage"] = "You don't have permission to view product details.";
                return RedirectToAction("Index");
            }

            ViewBag.ProductId = id;
            ViewBag.CurrentUser = CurrentUser;
            return View();
        }
    }
}

