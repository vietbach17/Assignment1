using BussinessLayer.Interfaces;
//using DataAccessLayer.Models;
using BussinessLayer.DTOs; // Thêm namespace DTOs vào
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace PresentationLayer.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IConfiguration _configuration;

        public SubscriptionController(ISubscriptionService subscriptionService, IConfiguration configuration)
        {
            _subscriptionService = subscriptionService;
            _configuration = configuration;
        }

        // Hàm Helper dùng chung để lấy nhanh UserId từ Cookie đăng nhập của hệ thống
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                // Nếu chưa login hệ thống (dành cho lúc chạy thử nghiệm độc lập), mặc định lấy ID của tài khoản Student mẫu là 3
                return 3;
            }
            return int.Parse(userIdClaim);
        }

        // ================== ZONE CHO STUDENT ==================

        // Xem danh sách các gói dịch vụ chatbot hiện có
        public IActionResult Index()
        {
            List<SubscriptionPlanDTO> plans = _subscriptionService.GetAllPlans();

            // Truyền PlanId hiện tại của Student để View đánh dấu gói đang dùng
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Student"))
            {
                int userId = GetCurrentUserId();
                var currentSub = _subscriptionService.GetStudentSubscription(userId);
                ViewBag.CurrentPlanId = currentSub?.SubscriptionPlanId ?? 0;
            }

            return View(plans); // Truyền danh sách sang View
        }

        // Xem thông tin gói hiện tại của cá nhân và số lượt câu hỏi còn lại
        [Authorize(Roles = "Student")]
        public IActionResult MySubscription()
        {
            int userId = GetCurrentUserId();

            // Nhận về DTO thay vì Entity
            var studentSubDto = _subscriptionService.GetStudentSubscription(userId);

            return View(studentSubDto);
        }

        // Thay thế Action BuyPlan cũ bằng code gọi link VNPay thật này:
        [Authorize(Roles = "Student")]
        [HttpPost]
        public IActionResult BuyPlan(int planId)
        {
            int userId = GetCurrentUserId();

            // Đọc cấu hình xem có bắt buộc dùng VNPay thật hay không
            var useVnPayReal = _configuration.GetValue<bool>("VNPAY:UseSandbox");

            if (useVnPayReal)
            {
                // --- LUỒNG VNPAY THẬT (Giữ nguyên code cũ của cậu) ---
                string returnUrl = Url.Action("VnPayReturn", "Subscription", null, Request.Scheme)!;
                string paymentUrl = _subscriptionService.CreateVnPayPaymentUrl(userId, planId, HttpContext, returnUrl);

                if (string.IsNullOrEmpty(paymentUrl))
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin gói dịch vụ.";
                    return RedirectToAction(nameof(Index));
                }
                return Redirect(paymentUrl);
            }
            else
            {
                // --- LUỒNG GIẢ LẬP BYPASS (Dành cho đêm nay để test sạch bug) ---
                // Thư viện VNPAY.NET cần các tham số description để xử lý, ta tự tạo Request giả lập luôn
                var mockPlan = _subscriptionService.GetPlanById(planId);
                if (mockPlan == null) return RedirectToAction(nameof(Index));

                // Tự tạo một URL callback giả lập chứa chuỗi Description đúng định dạng tài liệu yêu cầu
                // Định dạng: /Subscription/VnPayReturn?vnp_OrderInfo=PAY_USER_[userId]_PLAN_[planId]&vnp_ResponseCode=00
                string mockCallbackUrl = $"/Subscription/VnPayReturnFake?userId={userId}&planId={planId}";

                return RedirectToAction(nameof(BuyPlanFakeConfirmation), new { userId = userId, planId = planId });
            }
        }

        // ACTION MỚI: NHẬN PHẢN HỒI TỪ VNPAY
        [Authorize(Roles = "Student")]
        public IActionResult VnPayReturn()
        {
            try
            {
                // Truyền trực tiếp đối tượng Request của Controller vào Service xử lý theo tài liệu hướng dẫn
                bool isSuccess = _subscriptionService.ProcessVnPayReturn(this.Request);

                if (isSuccess)
                {
                    TempData["SuccessMessage"] = "Thanh toán qua cổng VNPay thành công! Gói dịch vụ đã được kích hoạt.";
                    return RedirectToAction(nameof(MySubscription));
                }

                TempData["ErrorMessage"] = "Giao dịch thanh toán thất bại hoặc phản hồi không hợp lệ.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi xử lý callback VNPay: {ex.Message} (Chi tiết: {ex.InnerException?.Message ?? "Không có"})";
            }
            return RedirectToAction(nameof(Index));
        }

        // ACTION MỚI: NHẬN THÔNG BÁO GIAO DỊCH TỪ SERVER VNPAY (Server-to-Server IPN)
        // Không dùng [Authorize] vì VNPay gọi trực tiếp từ backend của họ
        [HttpGet]
        public IActionResult VnPayIPN()
        {
            try
            {
                bool isSuccess = _subscriptionService.ProcessVnPayReturn(this.Request);
                if (isSuccess)
                {
                    return Json(new { RspCode = "00", Message = "Confirm Success" });
                }
                return Json(new { RspCode = "99", Message = "Confirm Failed" });
            }
            catch (Exception ex)
            {
                return Json(new { RspCode = "99", Message = ex.Message });
            }
        }
        // ================== ACTION MỚI: GIẢ LẬP TRẢ VỀ TỪ VNPAY (Dành cho test nhanh) ==================
        [Authorize(Roles = "Student")]
        public IActionResult BuyPlanFakeConfirmation(int userId, int planId)
        {
            var plan = _subscriptionService.GetPlanById(planId);
            ViewBag.UserId = userId;
            ViewBag.PlanId = planId;
            return View(plan);
        }

        [Authorize(Roles = "Student")]
        [HttpPost]
        public IActionResult ProcessFakePayment(int userId, int planId)
        {
            var plan = _subscriptionService.GetPlanById(planId);
            if (plan != null)
            {
                // 1. Ghi nhận lịch sử giao dịch thành công
                var transaction = new DataAccessLayer.Models.PaymentTransaction
                {
                    UserId = userId,
                    SubscriptionPlanId = planId,
                    Amount = plan.Price,
                    TransactionDate = DateTime.UtcNow,
                    Status = "Success"
                };
                _subscriptionService.AddTransaction(transaction); // Đảm bảo lưu lịch sử transaction

                // 2. Cập nhật hạn mức gói mới cho Sinh viên
                var currentSub = _subscriptionService.GetStudentSubscription(userId);
                if (currentSub != null)
                {
                    currentSub.SubscriptionPlanId = plan.Id;
                    currentSub.StartDate = DateTime.UtcNow;
                    currentSub.EndDate = DateTime.UtcNow.AddMonths(1);

                    currentSub.RemainingQuestions = plan.QuestionLimit;

                    // Gọi hàm lưu đè xuống Postgres
                    var entity = new DataAccessLayer.Models.StudentSubscription
                    {
                        Id = currentSub.Id,
                        UserId = currentSub.UserId,
                        SubscriptionPlanId = plan.Id, // Gán ID gói mới mua
                        StartDate = DateTime.UtcNow,
                        EndDate = plan.Id == 1 ? DateTime.MaxValue : DateTime.UtcNow.AddMonths(1), // Gói Free = vĩnh viễn
                        RemainingQuestions = plan.QuestionLimit, // Cập nhật số lượng câu hỏi mới theo gói
                        DailyResetTime = null // Reset chu kỳ daily khi đổi gói
                    };

                    _subscriptionService.SaveStudentSubscription(entity); // Lưu thay đổi xuống Database

                    TempData["SuccessMessage"] = $"⚡ Xác nhận thanh toán thành công! Gói {plan.Name} ({plan.QuestionLimit} câu) đã được kích hoạt.";
                    return RedirectToAction(nameof(MySubscription));
                }
            }

            TempData["ErrorMessage"] = "Thanh toán thất bại.";
            return RedirectToAction(nameof(Index));
        }

        // ================== ZONE CHO ADMIN (CRUD Gói) ==================

        [Authorize(Roles = "Admin")]
        public IActionResult AdminIndex()
        {
            var plans = _subscriptionService.GetAllPlans();
            return View(plans);
        }


        [Authorize(Roles = "Admin")]
        public IActionResult Admin()
        {
            var plans = _subscriptionService.GetAllPlans();
            return View(plans);
        }
        [Authorize(Roles = "Admin")]
        public IActionResult Create() => View();

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Create(SubscriptionPlanDTO planDto) // Đổi thành DTO
        {
            if (ModelState.IsValid)
            {
                _subscriptionService.CreatePlan(planDto); // Truyền DTO xuống Service
                TempData["SuccessMessage"] = "Tạo gói dịch vụ mới thành công!";
                return RedirectToAction(nameof(AdminIndex));
            }
            return View(planDto);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var planDto = _subscriptionService.GetPlanById(id); // Nhận về DTO từ Service
            if (planDto == null) return NotFound();
            return View(planDto);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Edit(SubscriptionPlanDTO planDto) // Đổi thành DTO
        {
            if (ModelState.IsValid)
            {
                _subscriptionService.UpdatePlan(planDto); // Truyền DTO xuống Service
                TempData["SuccessMessage"] = "Cập nhật thông tin gói thành công!";
                return RedirectToAction(nameof(AdminIndex));
            }
            return View(planDto);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Delete(int id)
        {
            _subscriptionService.DeletePlan(id);
            TempData["SuccessMessage"] = "Đã xóa gói dịch vụ thành công!";
            return RedirectToAction(nameof(AdminIndex));
        }
    }
}