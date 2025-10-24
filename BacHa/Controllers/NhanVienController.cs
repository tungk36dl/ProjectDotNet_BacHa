using Microsoft.AspNetCore.Mvc;

namespace BacHa.Controllers
{
    public class NhanVienController : Controller
    {
        public IActionResult Index()
        {
            // Kiểm tra quyền NhanVien
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Login", "Auth");
            }

            var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (userRole != "NhanVien")
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }
    }
}
