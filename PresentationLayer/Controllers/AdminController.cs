using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    // Sử dụng Attribute [Authorize(Roles = "Admin")] để chặn tất cả yêu cầu truy cập từ người dùng không phải Admin
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        public IActionResult Dashboard()
        {
            // Trả về trang quản trị dành riêng cho Admin
            return View();
        }
    }
}
