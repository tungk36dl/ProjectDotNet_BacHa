using Microsoft.AspNetCore.Mvc;

namespace BacHa.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            // Kiểm tra quyền Admin
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "Admin")
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
    }
}
