using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    public class HomeController : Controller
    {
        // Trang chủ mặc định: Chuyển hướng người dùng về trang đăng nhập nếu chưa xác thực, hoặc trang chức năng của họ
        public IActionResult Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Dashboard", "Admin");
                if (User.IsInRole("Lecturer"))
                    return RedirectToAction("Index", "Document");
                if (User.IsInRole("Student"))
                    return RedirectToAction("Chat", "Home");
            }
            return RedirectToAction("Login", "Auth");
        }

        // Chỉ cho phép sinh viên (Student) truy cập vào chức năng Chat trực tuyến
        [Authorize(Roles = "Student")]
        public IActionResult Chat()
        {
            // Trả về giao diện chat tương tác hiện đại của Sinh viên
            return View();
        }
    }
}
