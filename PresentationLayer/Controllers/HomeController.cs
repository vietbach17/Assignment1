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
        private readonly ISubjectService _subjectService;

        // Tiêm dịch vụ quản lý gói vào HomeController (Subcription)
        public HomeController(ISubscriptionService subscriptionService, IDocumentService documentService, ISubjectService subjectService)
        {
            _subscriptionService = subscriptionService;
            _documentService = documentService;
            _subjectService = subjectService;
        }

        // Hàm Helper lấy nhanh UserId từ Cookie đăng nhập
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return string.IsNullOrEmpty(userIdClaim) ? 3 : int.Parse(userIdClaim);
        }

        // Trang chủ mặc định: Chuyển hướng người dùng về trang đăng nhập nếu chưa xác thực, hoặc trang chức năng của họ
        public async Task<IActionResult> Index()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("Dashboard", "Admin");

                if (User.IsInRole("Lecturer"))
                {
                    var userId = GetCurrentUserId();
                    var myDocs     = (await _documentService.GetDocumentsByUploadedByUserAsync(userId)).ToList();
                    var mySubjects = (await _subjectService.GetSubjectsByLecturerIdAsync(userId, includeDeleted: false)).ToList();

                    ViewBag.LecturerDocuments    = myDocs;
                    ViewBag.LecturerRecentDocs   = myDocs.OrderByDescending(d => d.UploadedDate).Take(5).ToList();
                    ViewBag.LecturerSubjects     = mySubjects;
                    ViewBag.LecturerTotalDocs    = myDocs.Count;
                    ViewBag.LecturerIndexedDocs  = myDocs.Count(d => d.Status == BussinessLayer.DTOs.DocumentStatus.Indexed);
                    ViewBag.LecturerSubjectCount = mySubjects.Count;

                    return View("LecturerDashboard");
                }

                if (User.IsInRole("Student"))
                {
                    var subjects = await _subjectService.GetAllSubjectsAsync(includeDeleted: false);
                    ViewBag.SubjectCount = subjects.Count();
                    return RedirectToAction("Chat");
                }
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

        // --- HÀM TRỪ CÂU HỎI VỚI CƠ CHẾ RESET HÀNG NGÀY (24H) ---
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

            // 2. Lấy cấu hình gói
            var planDto = _subscriptionService.GetPlanById(currentSubDto.SubscriptionPlanId);
            if (planDto == null)
            {
                return Json(new { success = false, message = "Không tìm thấy cấu hình gói." });
            }

            // 3. Kiểm tra & thực hiện Daily Reset nếu đã qua 24h
            bool wasReset = false;
            if (planDto.QuestionLimit < 9999
                && currentSubDto.DailyResetTime.HasValue
                && DateTime.UtcNow >= currentSubDto.DailyResetTime.Value.AddHours(24))
            {
                // Đã qua 24h → Reset lại số câu hỏi
                currentSubDto.RemainingQuestions = planDto.QuestionLimit;
                currentSubDto.DailyResetTime = null;
                wasReset = true;

                // Lưu ngay trạng thái reset xuống DB
                var resetEntity = new DataAccessLayer.Models.StudentSubscription
                {
                    Id = currentSubDto.Id,
                    UserId = currentSubDto.UserId,
                    SubscriptionPlanId = currentSubDto.SubscriptionPlanId,
                    StartDate = currentSubDto.StartDate,
                    EndDate = currentSubDto.EndDate,
                    RemainingQuestions = planDto.QuestionLimit,
                    DailyResetTime = null
                };
                _subscriptionService.SaveStudentSubscription(resetEntity);
            }

            // 4. Kiểm tra còn lượt không
            if (planDto.QuestionLimit < 9999 && currentSubDto.RemainingQuestions <= 0)
            {
                // Tính thời gian còn lại trước khi reset
                string resetTimeIso = "";
                if (currentSubDto.DailyResetTime.HasValue)
                {
                    resetTimeIso = currentSubDto.DailyResetTime.Value.AddHours(24).ToString("o");
                }

                return Json(new
                {
                    success = false,
                    outOfQuota = true,
                    message = "Bạn đã hết lượt đặt câu hỏi hôm nay. Vui lòng chờ reset hoặc nâng cấp gói!",
                    resetTime = resetTimeIso
                });
            }

            // 5. Nếu gói không phải Vô hạn (Pro), tiến hành trừ 1 lượt hỏi
            if (planDto.QuestionLimit < 9999)
            {
                // Nếu chưa có DailyResetTime → đây là câu hỏi đầu tiên trong chu kỳ
                DateTime? newDailyResetTime = currentSubDto.DailyResetTime ?? DateTime.UtcNow;

                var entity = new DataAccessLayer.Models.StudentSubscription
                {
                    Id = currentSubDto.Id,
                    UserId = currentSubDto.UserId,
                    SubscriptionPlanId = currentSubDto.SubscriptionPlanId,
                    StartDate = currentSubDto.StartDate,
                    EndDate = currentSubDto.EndDate,
                    RemainingQuestions = currentSubDto.RemainingQuestions - 1,
                    DailyResetTime = newDailyResetTime
                };

                _subscriptionService.SaveStudentSubscription(entity);
                currentSubDto.RemainingQuestions = entity.RemainingQuestions;
                currentSubDto.DailyResetTime = newDailyResetTime;
            }

            // 6. Trả về kết quả kèm thời gian reset cho frontend
            string resetTimeResponse = "";
            if (currentSubDto.DailyResetTime.HasValue)
            {
                resetTimeResponse = currentSubDto.DailyResetTime.Value.AddHours(24).ToString("o");
            }

            return Json(new
            {
                success = true,
                remaining = currentSubDto.RemainingQuestions,
                resetTime = resetTimeResponse
            });
        }
    }
}
