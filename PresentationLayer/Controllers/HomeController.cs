using BussinessLayer.Interfaces;
using BussinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PresentationLayer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;

        // Tiêm dịch vụ quản lý gói vào HomeController (Subcription)
        public HomeController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        // Hàm Helper lấy nhanh UserId từ Cookie đăng nhập
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(userIdClaim) ? 3 : int.Parse(userIdClaim);
        }

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

        // --- HÀM MỚI CHÈN VÀO ĐỂ TRỪ CÂU HỎI KHI CHAT ---
        [Authorize(Roles = "Student")]
        [HttpPost]
        public IActionResult VerifyAndDeductQuestion()
        {
            int userId = GetCurrentUserId();
            var currentSub = _subscriptionService.GetStudentSubscription(userId);

            if (currentSub == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin gói dịch vụ." });
            }

            // Kiểm tra nếu đã hết lượt hỏi (và gói không phải Unlimited)
            if (currentSub.SubscriptionPlan.QuestionLimit < 9999 && currentSub.RemainingQuestions <= 0)
            {
                return Json(new
                {
                    success = false,
                    outOfQuota = true,
                    message = "Bạn đã hết lượt đặt câu hỏi trong tháng này. Vui lòng nâng cấp gói!"
                });
            }

            // Thực hiện trừ 1 câu hỏi nếu không phải gói Pro Unlimited
            if (currentSub.SubscriptionPlan.QuestionLimit < 9999)
            {
                currentSub.RemainingQuestions -= 1;
                _subscriptionService.SaveStudentSubscription(currentSub);
            }

            // Trả về kết quả hợp lệ kèm số lượt hỏi còn lại để lát UI hiển thị nếu thích
            return Json(new { success = true, remaining = currentSub.RemainingQuestions });
        }
    }
}
