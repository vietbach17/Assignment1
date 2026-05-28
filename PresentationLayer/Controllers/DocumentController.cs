using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    // Sử dụng Attribute [Authorize(Roles = "Lecturer")] để chỉ cho phép tài khoản có Role là Lecturer truy cập vào
    [Authorize(Roles = "Lecturer")]
    public class DocumentController : Controller
    {
        public IActionResult Index()
        {
            // Trả về trang quản lý tài liệu dành riêng cho Giảng viên (Lecturer)
            return View();
        }
    }
}
