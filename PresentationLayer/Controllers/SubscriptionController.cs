using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using DataAccessLayer.Models;
using BussinessLayer.Services;

namespace PresentationLayer.Controllers
{
    public class SubscriptionController : Controller
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
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

        // Xử lý luồng bấm mua gói (Giả lập tạo transaction thành công)
        [Authorize(Roles = "Student")]
        [HttpPost]
        public IActionResult BuyPlan(int planId)
        {
            int userId = GetCurrentUserId();
            bool result = _subscriptionService.PurchasePlan(userId, planId);

            if (result)
            {
                TempData["SuccessMessage"] = "Đăng ký gói hội viên thành công! Lượt câu hỏi đã được làm mới.";
                return RedirectToAction(nameof(MySubscription));
            }

            TempData["ErrorMessage"] = "Có lỗi xảy ra trong quá trình đăng ký gói.";
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