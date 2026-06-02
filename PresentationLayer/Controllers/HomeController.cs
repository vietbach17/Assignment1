using BussinessLayer.Interfaces;
using BussinessLayer.Services;
using BussinessLayer.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PresentationLayer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IDocumentService _documentService;

        // Tiêm dịch vụ quản lý gói vào HomeController (Subcription)
        public HomeController(ISubscriptionService subscriptionService, IDocumentService documentService)
        {
            _subscriptionService = subscriptionService;
            _documentService = documentService;
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
        public async Task<IActionResult> Chat()
        {
            var documents = await _documentService.GetAllDocumentsAsync(includeDeleted: false);
            ViewBag.Documents = documents.ToList();
            // Trả về giao diện chat tương tác hiện đại của Sinh viên
            return View();
        }

        // --- HÀM MỚI CHÈN VÀO ĐỂ TRỪ CÂU HỎI KHI CHAT ---
        [Authorize(Roles = "Student")]
        [HttpPost]
        public IActionResult VerifyAndDeductQuestion()
        {
            int userId = GetCurrentUserId();

            // 1. Lấy thông tin gói hiện tại của học sinh (Dạng DTO)
            var currentSubDto = _subscriptionService.GetStudentSubscription(userId);

            if (currentSubDto == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông tin gói dịch vụ." });
            }

            // 2. FIX LỖI FOREACH: Thay vì dùng foreach duyệt qua 1 object đơn lẻ, 
            // ta lấy trực tiếp thông tin cấu hình gói dựa trên SubscriptionPlanId có sẵn trong DTO
            var planDto = _subscriptionService.GetPlanById(currentSubDto.SubscriptionPlanId);
            if (planDto == null)
            {
                return Json(new { success = false, message = "Không tìm thấy cấu hình gói." });
            }

            // 3. FIX LỖI .SubscriptionPlan: Đọc trực tiếp giới hạn câu hỏi từ planDto vừa lấy được
            // Kiểm tra xem gói có bị giới hạn và học sinh đã dùng hết lượt chưa
            if (planDto.QuestionLimit < 9999 && currentSubDto.RemainingQuestions <= 0)
            {
                return Json(new
                {
                    success = false,
                    outOfQuota = true,
                    message = "Bạn đã hết lượt đặt câu hỏi trong tháng này. Vui lòng nâng cấp gói!"
                });
            }

            // 4. FIX LỖI ÉP KIỂU: Nếu gói không phải Vô hạn (Pro), tiến hành trừ 1 lượt hỏi
            if (planDto.QuestionLimit < 9999)
            {
                // Khởi tạo một đối tượng Entity thực tế của tầng DataAccessLayer để truyền xuống hàm Save
                var entity = new DataAccessLayer.Models.StudentSubscription
                {
                    Id = currentSubDto.Id,
                    UserId = currentSubDto.UserId,
                    SubscriptionPlanId = currentSubDto.SubscriptionPlanId,
                    StartDate = currentSubDto.StartDate,
                    EndDate = currentSubDto.EndDate,
                    RemainingQuestions = currentSubDto.RemainingQuestions - 1 // Thực hiện trừ câu hỏi
                };

                // Gọi hàm lưu của hệ thống bằng cách truyền đúng kiểu đối tượng Entity gốc
                _subscriptionService.SaveStudentSubscription(entity);

                // Cập nhật lại số lượng câu hỏi mới vào biến DTO để phản hồi về cho giao diện hiển thị
                currentSubDto.RemainingQuestions = entity.RemainingQuestions;
            }

            return Json(new { success = true, remaining = currentSubDto.RemainingQuestions });
        }
    }
}
