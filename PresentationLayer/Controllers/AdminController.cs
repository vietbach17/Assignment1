using BussinessLayer.Services;
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

        public AdminController(IDocumentService documentService, IRoleService roleService)
        {
            _documentService = documentService;
            _roleService = roleService;
        }

        public async Task<IActionResult> Dashboard()
        {
            // Lấy danh sách toàn bộ tài liệu đã upload để hiển thị cho Admin
            var documents = await _documentService.GetAllDocumentsAsync(includeDeleted: false);
            
            // Lấy danh sách toàn bộ Role
            ViewBag.Roles = await _roleService.GetAllRolesAsync();
            
            // Trả về trang quản trị dành riêng cho Admin kèm danh sách tài liệu
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
    }
}
