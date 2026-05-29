using System.Security.Claims;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    /// <summary>
    /// Controller quản lý CRUD operations cho Subject (Môn học)
    /// Yêu cầu authentication cho tất cả actions
    /// Admin và Lecturer có quyền Create/Edit/Delete
    /// Tất cả authenticated users có quyền xem (Index, Details)
    /// </summary>
    [Authorize]
    public class SubjectsController : Controller
    {
        private readonly ISubjectService _subjectService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SubjectsController(
            ISubjectService subjectService,
            IHttpContextAccessor httpContextAccessor)
        {
            _subjectService = subjectService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// GET: /Subjects/Index
        /// Hiển thị danh sách tất cả các Subject
        /// Accessible by all authenticated users
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var subjects = await _subjectService.GetAllSubjectsAsync(includeDeleted: false);
            return View(subjects);
        }

        /// <summary>
        /// GET: /Subjects/Details/{id}
        /// Hiển thị chi tiết Subject kèm theo danh sách Chapters
        /// Accessible by all authenticated users
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var subject = await _subjectService.GetSubjectWithChaptersAsync(id, includeDeleted: false);
            
            if (subject == null)
            {
                TempData["ErrorMessage"] = "Subject not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(subject);
        }

        /// <summary>
        /// GET: /Subjects/Create
        /// Hiển thị form tạo Subject mới
        /// Chỉ Admin có quyền truy cập
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            // Populate danh sách Lecturers cho dropdown
            await PopulateLecturersDropdown();
            return View(new CreateSubjectDto());
        }

        /// <summary>
        /// POST: /Subjects/Create
        /// Xử lý tạo Subject mới và assign cho Lecturer
        /// Chỉ Admin có quyền truy cập
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(CreateSubjectDto dto, int? assignedLecturerId)
        {
            if (!ModelState.IsValid)
            {
                await PopulateLecturersDropdown();
                return View(dto);
            }

            // Lấy userId từ Claims
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Unable to identify current user.";
                return RedirectToAction(nameof(Index));
            }

            // Gọi service để tạo Subject
            var (success, message, subject) = await _subjectService.CreateSubjectAsync(dto, userId.Value);

            if (!success)
            {
                // Nếu thất bại, hiển thị error message và giữ lại form
                ModelState.AddModelError(string.Empty, message);
                await PopulateLecturersDropdown();
                return View(dto);
            }

            // Assign Lecturer nếu được chọn
            if (assignedLecturerId.HasValue && subject != null)
            {
                // Check if lecturer is already assigned to avoid duplicate
                var isAlreadyAssigned = await _subjectService.IsLecturerAssignedToSubjectAsync(subject.Id, assignedLecturerId.Value);
                if (!isAlreadyAssigned)
                {
                    await _subjectService.AssignLecturerAsync(subject.Id, assignedLecturerId.Value);
                }
            }

            // Thành công - hiển thị success message và redirect về Index
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// GET: /Subjects/Edit/{id}
        /// Hiển thị form chỉnh sửa Subject
        /// Chỉ Admin có quyền truy cập
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(id, includeDeleted: false);
            
            if (subject == null)
            {
                TempData["ErrorMessage"] = "Subject not found.";
                return RedirectToAction(nameof(Index));
            }

            // Populate danh sách Lecturers cho dropdown
            await PopulateLecturersDropdown();

            // Lấy danh sách Lecturers đã được assign
            var assignedLecturers = await _subjectService.GetAssignedLecturersAsync(id);
            ViewBag.AssignedLecturerIds = assignedLecturers.Select(l => l.Id).ToList();

            // Map SubjectDto sang UpdateSubjectDto
            var updateDto = new UpdateSubjectDto
            {
                Id = subject.Id,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName,
                Description = subject.Description
            };

            return View(updateDto);
        }

        /// <summary>
        /// POST: /Subjects/Edit/{id}
        /// Xử lý cập nhật Subject và lecturer assignments
        /// Chỉ Admin có quyền truy cập
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, UpdateSubjectDto dto, int? assignedLecturerId)
        {
            if (id != dto.Id)
            {
                TempData["ErrorMessage"] = "Subject ID mismatch.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                await PopulateLecturersDropdown();
                return View(dto);
            }

            // Lấy userId từ Claims
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Unable to identify current user.";
                return RedirectToAction(nameof(Index));
            }

            // Gọi service để update Subject
            var (success, message, subject) = await _subjectService.UpdateSubjectAsync(dto, userId.Value);

            if (!success)
            {
                // Nếu thất bại, hiển thị error message và giữ lại form
                ModelState.AddModelError(string.Empty, message);
                await PopulateLecturersDropdown();
                return View(dto);
            }

            // Update lecturer assignment nếu được chọn
            if (assignedLecturerId.HasValue)
            {
                // Xóa tất cả assignments cũ
                await _subjectService.ClearLecturerAssignmentsAsync(id);
                // Thêm assignment mới
                await _subjectService.AssignLecturerAsync(id, assignedLecturerId.Value);
            }

            // Thành công - hiển thị success message và redirect về Index
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// GET: /Subjects/Delete/{id}
        /// Hiển thị trang xác nhận xóa Subject
        /// Chỉ Admin có quyền truy cập
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var subject = await _subjectService.GetSubjectWithChaptersAsync(id, includeDeleted: false);
            
            if (subject == null)
            {
                TempData["ErrorMessage"] = "Subject not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(subject);
        }

        /// <summary>
        /// POST: /Subjects/Delete/{id}
        /// Xử lý soft delete Subject (cascade delete chapters)
        /// Chỉ Admin có quyền truy cập
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Gọi service để soft delete Subject (không check HasChapters nữa)
            var (success, message) = await _subjectService.SoftDeleteSubjectAsync(id);

            if (!success)
            {
                TempData["ErrorMessage"] = message;
            }
            else
            {
                TempData["SuccessMessage"] = message;
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Helper method để lấy userId từ HttpContext Claims
        /// </summary>
        private int? GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            return null;
        }

        /// <summary>
        /// Helper method để populate ViewBag.Lecturers cho dropdown trong Create/Edit forms
        /// </summary>
        private async Task PopulateLecturersDropdown()
        {
            var lecturers = await _subjectService.GetAllLecturersAsync();
            ViewBag.Lecturers = lecturers;
        }
    }
}
