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
        private readonly ISubjectService _subjectService;

        public AdminController(
            IDocumentService documentService,
            IRoleService roleService,
            IUserService userService,
            ISubscriptionService subscriptionService,
            ISubjectService subjectService)
        {
            _documentService = documentService;
            _roleService = roleService;
            _userService = userService;
            _subscriptionService = subscriptionService;
            _subjectService = subjectService;
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
    }
}
