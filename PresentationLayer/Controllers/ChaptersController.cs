using System.Security.Claims;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers
{
    /// <summary>
    /// Controller quản lý CRUD operations cho Chapter (Chương học)
    /// Yêu cầu authentication cho tất cả actions
    /// Admin và Lecturer có quyền Create/Edit/Delete
    /// Tất cả authenticated users có quyền xem (Index, Details)
    /// </summary>
    [Authorize(Roles = "Lecturer,Admin")]
    public class ChaptersController : Controller
    {
        private readonly IChapterService _chapterService;
        private readonly ISubjectService _subjectService;
        private readonly IDocumentService _documentService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ChaptersController(
            IChapterService chapterService, 
            ISubjectService subjectService, 
            IDocumentService documentService,
            IHttpContextAccessor httpContextAccessor)
        {
            _chapterService = chapterService;
            _subjectService = subjectService;
            _documentService = documentService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// GET: /Chapters/Index?subjectId={subjectId}
        /// Hiển thị danh sách tất cả các Chapter thuộc một Subject
        /// Accessible by all authenticated users
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(int? subjectId)
        {
            if (subjectId == null)
            {
                TempData["ErrorMessage"] = "Subject ID is required.";
                return RedirectToAction("Index", "Subjects");
            }

            // Kiểm tra Subject có tồn tại không
            var subject = await _subjectService.GetSubjectByIdAsync(subjectId.Value, includeDeleted: false);
            if (subject == null)
            {
                TempData["ErrorMessage"] = "Subject not found.";
                return RedirectToAction("Index", "Subjects");
            }

            // Lấy danh sách Chapters thuộc Subject
            var chapters = await _chapterService.GetChaptersBySubjectIdAsync(subjectId.Value, includeDeleted: false);
            
            // Truyền thông tin Subject qua ViewBag để hiển thị trên view
            ViewBag.SubjectId = subject.Id;
            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectName = subject.SubjectName;

            // Kiểm tra quyền của user để hiển thị button Create/Edit/Delete
            ViewBag.CanManageChapters = await IsAuthorizedForSubject(subjectId.Value);

            return View(chapters);
        }

        /// <summary>
        /// GET: /Chapters/Details/{id}
        /// Hiển thị chi tiết Chapter kèm theo thông tin Subject
        /// Accessible by all authenticated users
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var chapter = await _chapterService.GetChapterWithSubjectAsync(id, includeDeleted: false);
            
            if (chapter == null)
            {
                TempData["ErrorMessage"] = "Chapter not found.";
                return RedirectToAction("Index", "Subjects");
            }

            ViewBag.Documents = await _documentService.GetDocumentsByChapterAsync(id);
            ViewBag.CanManageChapter = await IsAuthorizedForChapter(id);

            return View(chapter);
        }

        /// <summary>
        /// GET: /Chapters/Create?subjectId={subjectId}
        /// Hiển thị form tạo Chapter mới
        /// Chỉ Admin và Lecturer có quyền truy cập
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Create(int? subjectId)
        {
            if (subjectId == null)
            {
                TempData["ErrorMessage"] = "Subject ID is required.";
                return RedirectToAction("Index", "Subjects");
            }

            // Kiểm tra Subject có tồn tại không
            var subject = await _subjectService.GetSubjectByIdAsync(subjectId.Value, includeDeleted: false);
            if (subject == null)
            {
                TempData["ErrorMessage"] = "Subject not found.";
                return RedirectToAction("Index", "Subjects");
            }

            // Authorization check: Lecturer phải được assign vào Subject
            if (!await IsAuthorizedForSubject(subjectId.Value))
            {
                TempData["ErrorMessage"] = "You are not authorized to create chapters for this subject.";
                return RedirectToAction("Index", "Subjects");
            }

            // Populate ViewBag.Subjects cho dropdown
            await PopulateSubjectsDropdown();

            // Tạo DTO với SubjectId được pre-fill
            var createDto = new CreateChapterDto
            {
                SubjectId = subjectId.Value
            };

            return View(createDto);
        }

        /// <summary>
        /// POST: /Chapters/Create
        /// Xử lý tạo Chapter mới
        /// Chỉ Admin và Lecturer có quyền truy cập
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Create(CreateChapterDto dto)
        {
            // Authorization check: Lecturer phải được assign vào Subject
            if (!await IsAuthorizedForSubject(dto.SubjectId))
            {
                TempData["ErrorMessage"] = "You are not authorized to create chapters for this subject.";
                return RedirectToAction("Index", "Subjects");
            }

            if (!ModelState.IsValid)
            {
                // Populate lại dropdown nếu validation fail
                await PopulateSubjectsDropdown();
                return View(dto);
            }

            // Lấy userId từ Claims
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Unable to identify current user.";
                return RedirectToAction("Index", "Subjects");
            }

            // Gọi service để tạo Chapter
            var (success, message, chapter) = await _chapterService.CreateChapterAsync(dto, userId.Value);

            if (!success)
            {
                // Nếu thất bại, hiển thị error message và giữ lại form
                ModelState.AddModelError(string.Empty, message);
                await PopulateSubjectsDropdown();
                return View(dto);
            }

            // Thành công - hiển thị success message và redirect về Index của Subject
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index), new { subjectId = dto.SubjectId });
        }

        /// <summary>
        /// GET: /Chapters/Edit/{id}
        /// Hiển thị form chỉnh sửa Chapter
        /// Chỉ Admin và Lecturer có quyền truy cập
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Edit(int id)
        {
            var chapter = await _chapterService.GetChapterByIdAsync(id, includeDeleted: false);
            
            if (chapter == null)
            {
                TempData["ErrorMessage"] = "Chapter not found.";
                return RedirectToAction("Index", "Subjects");
            }

            // Authorization check: Lecturer phải được assign vào Subject của Chapter
            if (!await IsAuthorizedForChapter(id))
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this chapter.";
                return RedirectToAction("Index", "Subjects");
            }

            // Populate ViewBag.Subjects cho dropdown
            await PopulateSubjectsDropdown();

            // Map ChapterDto sang UpdateChapterDto
            var updateDto = new UpdateChapterDto
            {
                Id = chapter.Id,
                ChapterNumber = chapter.ChapterNumber,
                ChapterTitle = chapter.ChapterTitle,
                Description = chapter.Description,
                SubjectId = chapter.SubjectId
            };

            return View(updateDto);
        }

        /// <summary>
        /// POST: /Chapters/Edit/{id}
        /// Xử lý cập nhật Chapter
        /// Chỉ Admin và Lecturer có quyền truy cập
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Edit(int id, UpdateChapterDto dto)
        {
            if (id != dto.Id)
            {
                TempData["ErrorMessage"] = "Chapter ID mismatch.";
                return RedirectToAction("Index", "Subjects");
            }

            // Authorization check: Lecturer phải được assign vào Subject của Chapter
            if (!await IsAuthorizedForChapter(id))
            {
                TempData["ErrorMessage"] = "You are not authorized to edit this chapter.";
                return RedirectToAction("Index", "Subjects");
            }

            if (!ModelState.IsValid)
            {
                // Populate lại dropdown nếu validation fail
                await PopulateSubjectsDropdown();
                return View(dto);
            }

            // Lấy userId từ Claims
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Unable to identify current user.";
                return RedirectToAction("Index", "Subjects");
            }

            // Gọi service để update Chapter
            var (success, message, chapter) = await _chapterService.UpdateChapterAsync(dto, userId.Value);

            if (!success)
            {
                // Nếu thất bại, hiển thị error message và giữ lại form
                ModelState.AddModelError(string.Empty, message);
                await PopulateSubjectsDropdown();
                return View(dto);
            }

            // Thành công - hiển thị success message và redirect về Index của Subject
            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index), new { subjectId = dto.SubjectId });
        }

        /// <summary>
        /// GET: /Chapters/Delete/{id}
        /// Hiển thị trang xác nhận xóa Chapter
        /// Chỉ Admin và Lecturer có quyền truy cập
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> Delete(int id)
        {
            var chapter = await _chapterService.GetChapterWithSubjectAsync(id, includeDeleted: false);
            
            if (chapter == null)
            {
                TempData["ErrorMessage"] = "Chapter not found.";
                return RedirectToAction("Index", "Subjects");
            }

            // Authorization check: Lecturer phải được assign vào Subject của Chapter
            if (!await IsAuthorizedForChapter(id))
            {
                TempData["ErrorMessage"] = "You are not authorized to delete this chapter.";
                return RedirectToAction("Index", "Subjects");
            }

            return View(chapter);
        }

        /// <summary>
        /// POST: /Chapters/Delete/{id}
        /// Xử lý soft delete Chapter
        /// Chỉ Admin và Lecturer có quyền truy cập
        /// </summary>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Lecturer")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // Authorization check: Lecturer phải được assign vào Subject của Chapter
            if (!await IsAuthorizedForChapter(id))
            {
                TempData["ErrorMessage"] = "You are not authorized to delete this chapter.";
                return RedirectToAction("Index", "Subjects");
            }

            // Lấy thông tin Chapter trước khi xóa để biết SubjectId
            var chapter = await _chapterService.GetChapterByIdAsync(id, includeDeleted: false);
            int? subjectId = chapter?.SubjectId;

            // Gọi service để soft delete Chapter
            var (success, message) = await _chapterService.SoftDeleteChapterAsync(id);

            if (!success)
            {
                TempData["ErrorMessage"] = message;
            }
            else
            {
                TempData["SuccessMessage"] = message;
            }

            // Redirect về Index của Subject nếu có, nếu không thì về Subjects Index
            if (subjectId.HasValue)
            {
                return RedirectToAction(nameof(Index), new { subjectId = subjectId.Value });
            }

            return RedirectToAction("Index", "Subjects");
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
        /// Helper method để populate ViewBag.Subjects cho dropdown trong Create/Edit forms
        /// </summary>
        private async Task PopulateSubjectsDropdown()
        {
            var subjects = await _subjectService.GetAllSubjectsAsync(includeDeleted: false);
            ViewBag.Subjects = subjects;
        }

        /// <summary>
        /// Helper method để kiểm tra authorization cho Chapter
        /// Admin có full access
        /// Lecturer chỉ có access nếu được assign vào Subject của Chapter
        /// </summary>
        /// <param name="chapterId">ID của Chapter cần kiểm tra</param>
        /// <returns>True nếu user có quyền, False nếu không</returns>
        private async Task<bool> IsAuthorizedForChapter(int chapterId)
        {
            // Admin có full access
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            // Nếu không phải Admin hoặc Lecturer, deny access
            if (!User.IsInRole("Lecturer"))
            {
                return false;
            }

            // Lecturer: cần check xem có được assign vào Subject của Chapter không
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return false;
            }

            // Lấy Chapter để biết SubjectId
            var chapter = await _chapterService.GetChapterByIdAsync(chapterId, includeDeleted: false);
            if (chapter == null)
            {
                return false;
            }

            // Check xem Lecturer có được assign vào Subject này không
            return await _subjectService.IsLecturerAssignedToSubjectAsync(chapter.SubjectId, userId.Value);
        }

        /// <summary>
        /// Helper method để kiểm tra authorization cho Subject (khi tạo Chapter mới)
        /// Admin có full access
        /// Lecturer chỉ có access nếu được assign vào Subject
        /// </summary>
        /// <param name="subjectId">ID của Subject cần kiểm tra</param>
        /// <returns>True nếu user có quyền, False nếu không</returns>
        private async Task<bool> IsAuthorizedForSubject(int subjectId)
        {
            // Admin có full access
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            // Nếu không phải Admin hoặc Lecturer, deny access
            if (!User.IsInRole("Lecturer"))
            {
                return false;
            }

            // Lecturer: cần check xem có được assign vào Subject không
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return false;
            }

            // Check xem Lecturer có được assign vào Subject này không
            return await _subjectService.IsLecturerAssignedToSubjectAsync(subjectId, userId.Value);
        }
    }
}
