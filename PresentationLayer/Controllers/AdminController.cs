using BussinessLayer.DTOs;
using BussinessLayer.Services;
using BussinessLayer.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    // Sử dụng Attribute [Authorize(Roles = "Admin")] để chặn tất cả yêu cầu truy cập từ người dùng không phải Admin
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IDocumentService _documentService;
        private readonly IRoleService _roleService;
        private readonly IUserService _userService;
        private readonly ISubscriptionService _subscriptionService;
        private readonly BussinessLayer.Services.IEmailService _emailService;
        private readonly ISubjectService _subjectService;
        private readonly IChunkSettingsService _chunkSettingsService;
        public AdminController(
            IDocumentService documentService,
            IRoleService roleService,
            IUserService userService,
            ISubscriptionService subscriptionService,
            ISubjectService subjectService,
            IChunkSettingsService chunkSettingsService,
            BussinessLayer.Services.IEmailService emailService)
        {
            _documentService = documentService;
            _roleService = roleService;
            _userService = userService;
            _subscriptionService = subscriptionService;
            _emailService = emailService;
            _subjectService = subjectService;
            _chunkSettingsService = chunkSettingsService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var documents    = await _documentService.GetAllDocumentsAsync(includeDeleted: false);
            var users        = await _userService.GetAllUsersAsync();
            var subjects     = await _subjectService.GetAllSubjectsAsync(includeDeleted: false);
            var plans        = _subscriptionService.GetAllPlans();

            ViewBag.Roles = await _roleService.GetAllRolesAsync();

            // ── Thống kê người dùng ──
            ViewBag.TotalUsers    = users.Count();
            ViewBag.ActiveUsers   = users.Count(u => !u.IsDeleted);
            ViewBag.DisabledUsers = users.Count(u => u.IsDeleted);

            // ── Thống kê môn học & tài liệu ──
            ViewBag.TotalSubjects  = subjects.Count();
            ViewBag.TotalDocuments = documents.Count();

            // ── Thống kê subscription ──
            ViewBag.TotalPlans = plans.Count;

            // ── Thống kê tài chính ──
            var transactions        = _subscriptionService.GetAllTransactions();
            var successTransactions = transactions.Where(t => t.Status == "Success").ToList();
            decimal totalRevenue    = successTransactions.Sum(t => t.Amount);
            decimal netProfit       = totalRevenue * 0.85m;

            ViewBag.Transactions             = transactions;
            ViewBag.TotalRevenue             = totalRevenue;
            ViewBag.NetProfit                = netProfit;
            ViewBag.SuccessTransactionCount  = successTransactions.Count;

            return View(documents);
        }

        // --- Role Management Actions ---
        
        [HttpGet]
        public IActionResult CreateRole()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                ModelState.AddModelError("", "Tên vai trò không được để trống");
                return View();
            }

            var success = await _roleService.CreateRoleAsync(roleName);
            if (success)
            {
                TempData["SuccessMsg"] = "Thêm vai trò thành công!";
                return RedirectToAction("Dashboard");
            }

            ModelState.AddModelError("", "Vai trò này đã tồn tại!");
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> EditRole(int id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null) return NotFound();
            return View(role);
        }

        [HttpPost]
        public async Task<IActionResult> EditRole(int id, string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                ModelState.AddModelError("", "Tên vai trò không được để trống");
                var role = await _roleService.GetRoleByIdAsync(id);
                return View(role);
            }

            var success = await _roleService.UpdateRoleAsync(id, roleName);
            if (success)
            {
                TempData["SuccessMsg"] = "Cập nhật vai trò thành công!";
                return RedirectToAction("Dashboard");
            }

            ModelState.AddModelError("", "Không thể cập nhật vai trò (có thể trùng tên hoặc vai trò này được bảo vệ).");
            var currentRole = await _roleService.GetRoleByIdAsync(id);
            return View(currentRole);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var success = await _roleService.DeleteRoleAsync(id);
            if (success)
            {
                TempData["SuccessMsg"] = "Đã xoá vai trò thành công!";
            }
            else
            {
                TempData["ErrorMsg"] = "Không thể xoá vai trò này (vai trò mặc định hoặc đang có User sử dụng).";
            }
            return RedirectToAction("Dashboard");
        }

        // --- User Management Actions ---
        
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            var users = await _userService.GetAllUsersAsync();
            // Truyền danh sách roles cho dropdown edit
            ViewBag.Roles = await _roleService.GetAllRolesAsync();
            return View(users);
        }

        [HttpGet]
        public async Task<IActionResult> UserDetails(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            ViewBag.Roles = await _roleService.GetAllRolesAsync();
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUser(UpdateUserDto dto)
        {
            if (!ModelState.IsValid)
            {
                TempData["UserErrorMsg"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction(nameof(Users));
            }

            var (success, message) = await _userService.UpdateUserAsync(dto);
            if (success) TempData["UserSuccessMsg"] = message;
            else TempData["UserErrorMsg"] = message;

            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableUser(int id)
        {
            var (success, message) = await _userService.DisableUserAsync(id);
            if (success) TempData["UserSuccessMsg"] = message;
            else TempData["UserErrorMsg"] = message;
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestoreUser(int id)
        {
            var (success, message) = await _userService.RestoreUserAsync(id);
            if (success) TempData["UserSuccessMsg"] = message;
            else TempData["UserErrorMsg"] = message;
            return RedirectToAction(nameof(Users));
        }

        // --- Subscription Plan Management Actions ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateSubscriptionPlan(SubscriptionPlanDTO planDto)
        {
            if (!ModelState.IsValid)
            {
                TempData["SubErrorMsg"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(SubscriptionPlans));
            }

            try
            {
                _subscriptionService.CreatePlan(planDto);
                TempData["SubSuccessMsg"] = $"Đã tạo gói \"{planDto.Name}\" thành công!";
            }
            catch (Exception ex)
            {
                TempData["SubErrorMsg"] = $"Lỗi khi tạo gói: {ex.Message}";
            }

            return RedirectToAction(nameof(SubscriptionPlans));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditSubscriptionPlan(SubscriptionPlanDTO planDto, bool applyToExistingStudents = false)
        {
            if (!ModelState.IsValid)
            {
                TempData["SubErrorMsg"] = "Dữ liệu không hợp lệ. Vui lòng kiểm tra lại.";
                return RedirectToAction(nameof(SubscriptionPlans));
            }

            var existing = _subscriptionService.GetPlanById(planDto.Id);
            if (existing == null)
            {
                TempData["SubErrorMsg"] = "Không tìm thấy gói subscription.";
                return RedirectToAction(nameof(SubscriptionPlans));
            }

            try
            {
                _subscriptionService.UpdatePlanWithOptions(planDto, applyToExistingStudents);
                var note = applyToExistingStudents ? " (đã cập nhật lượt cho sinh viên đang dùng gói này)" : "";
                TempData["SubSuccessMsg"] = $"Đã cập nhật gói \"{planDto.Name}\" thành công!{note}";
            }
            catch (Exception ex)
            {
                TempData["SubErrorMsg"] = $"Lỗi khi cập nhật gói: {ex.Message}";
            }

            return RedirectToAction(nameof(SubscriptionPlans));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteSubscriptionPlan(int id)
        {
            var plan = _subscriptionService.GetPlanById(id);
            if (plan == null)
            {
                TempData["SubErrorMsg"] = "Không tìm thấy gói subscription.";
                return RedirectToAction(nameof(SubscriptionPlans));
            }

            try
            {
                _subscriptionService.DeletePlan(id);
                TempData["SubSuccessMsg"] = $"Đã xóa gói \"{plan.Name}\" thành công!";
            }
            catch (Exception ex)
            {
                TempData["SubErrorMsg"] = $"Không thể xóa gói này (có thể đang được sinh viên sử dụng): {ex.Message}";
            }

            return RedirectToAction(nameof(SubscriptionPlans));
        }

        [HttpGet]
        public IActionResult SubscriptionPlans()
        {
            var plans = _subscriptionService.GetAllPlans();
            return View(plans);
        }

        // --- Notifications ---
        [HttpGet]
        public async Task<IActionResult> Notify()
        {
            // Provide available subscription plans for targeting
            var plans = _subscriptionService.GetAllPlans();
            ViewBag.Plans = plans;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendNotification(string title, string message, string target = "all", int? planId = null)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMsg"] = "Tiêu đề và nội dung không được để trống.";
                return RedirectToAction("Notify");
            }

            var users = (await _userService.GetAllUsersAsync()).Where(u => !u.IsDeleted && u.RoleName == "Student");
            var recipients = new List<string>();

            if (target == "plan" && planId.HasValue)
            {
                foreach (var u in users)
                {
                    var sub = _subscriptionService.GetStudentSubscription(u.Id);
                    if (sub != null && sub.SubscriptionPlanId == planId.Value && !string.IsNullOrEmpty(u.Email))
                    {
                        recipients.Add(u.Email);
                    }
                }
            }
            else
            {
                recipients = users.Where(u => !string.IsNullOrEmpty(u.Email)).Select(u => u.Email).ToList();
            }

            int sent = 0;
            foreach (var email in recipients)
            {
                try
                {
                    await _emailService.SendEmailAsync(email, title, message);
                    sent++;
                }
                catch
                {
                    // ignore per-recipient errors
                }
            }

            TempData["SuccessMsg"] = $"Đã gửi thông báo đến {sent} sinh viên.";
            return RedirectToAction("Dashboard");
        }

        // --- Chunk Settings ---
        [HttpGet]
        public IActionResult ChunkSettings()
        {
            var settings = _chunkSettingsService.GetSettings();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChunkSettings(ChunkSettingsDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            await _chunkSettingsService.SaveSettingsAsync(dto);
            TempData["SuccessMsg"] = "Cập nhật cấu hình Chunk AI thành công!";
            return RedirectToAction("ChunkSettings");
        }
    }
}
