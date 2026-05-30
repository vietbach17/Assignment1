using BussinessLayer.Interfaces;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
            var plans = _subscriptionService.GetAllPlans();
            return View(plans);
        }

        // Xem thông tin gói hiện tại của cá nhân và số lượt câu hỏi còn lại
        [Authorize(Roles = "Student")]
        public IActionResult MySubscription()
        {
            int userId = GetCurrentUserId();
            var studentSub = _subscriptionService.GetStudentSubscription(userId);
            return View(studentSub);
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
            // Truyền trực tiếp đối tượng Request của Controller vào Service xử lý theo tài liệu hướng dẫn
            bool isSuccess = _subscriptionService.ProcessVnPayReturn(this.Request);

            if (isSuccess)
            {
                TempData["SuccessMessage"] = "Thanh toán qua cổng VNPay thành công! Gói dịch vụ đã được kích hoạt.";
                return RedirectToAction(nameof(MySubscription));
            }

            TempData["ErrorMessage"] = "Giao dịch thanh toán thất bại hoặc chữ ký bảo mật không hợp lệ.";
            return RedirectToAction(nameof(Index));
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

                    // FIX LỖI 1: Cập nhật số câu hỏi còn lại bằng đúng giới hạn của gói mới mua (Ví dụ: Basic = 20 câu)
                    currentSub.RemainingQuestions = plan.QuestionLimit;

                    // Gọi hàm lưu đè xuống Postgres
                    _subscriptionService.SaveStudentSubscription(currentSub);

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
        public IActionResult Create() => View();

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Create(SubscriptionPlan plan)
        {
            if (ModelState.IsValid)
            {
                _subscriptionService.CreatePlan(plan);
                TempData["SuccessMessage"] = "Tạo gói dịch vụ mới thành công!";
                return RedirectToAction(nameof(AdminIndex));
            }
            return View(plan);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var plan = _subscriptionService.GetPlanById(id);
            if (plan == null) return NotFound();
            return View(plan);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult Edit(SubscriptionPlan plan)
        {
            if (ModelState.IsValid)
            {
                _subscriptionService.UpdatePlan(plan);
                TempData["SuccessMessage"] = "Cập nhật thông tin gói thành công!";
                return RedirectToAction(nameof(AdminIndex));
            }
            return View(plan);
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