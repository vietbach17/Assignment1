using System.Security.Claims;
using BussinessLayer.DTOs;
using BussinessLayer.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace PresentationLayer.Controllers
{
    /// <summary>
    /// Controller quản lý tài liệu PDF/DOCX/PPTX dành cho Lecturer
    /// Chức năng: Upload, Xem danh sách, Chi tiết, Xoá, Cập nhật trạng thái
    /// </summary>
    [Authorize(Roles = "Lecturer,Admin")]
    public class DocumentController : Controller
    {
        private readonly IDocumentService _documentService;
        private readonly ISubjectService _subjectService;
        private readonly IChapterService _chapterService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public DocumentController(
            IDocumentService documentService,
            ISubjectService subjectService,
            IChapterService chapterService,
            IWebHostEnvironment webHostEnvironment,
            IHttpContextAccessor httpContextAccessor)
        {
            _documentService = documentService;
            _subjectService = subjectService;
            _chapterService = chapterService;
            _webHostEnvironment = webHostEnvironment;
            _httpContextAccessor = httpContextAccessor;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: /Document
        // Danh sách tài liệu, có thể filter theo SubjectId
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Index(int? subjectId, DocumentStatus? status)
        {
            IEnumerable<DocumentDto> documents;
            var userId = GetCurrentUserId();

            if (User.IsInRole("Lecturer") && userId.HasValue)
                documents = await _documentService.GetDocumentsByUploadedByUserAsync(userId.Value);
            else if (subjectId.HasValue)
                documents = await _documentService.GetDocumentsBySubjectAsync(subjectId.Value);
            else
                documents = await _documentService.GetAllDocumentsAsync(includeDeleted: false);

            // Filter theo trạng thái nếu có
            if (User.IsInRole("Lecturer") && subjectId.HasValue)
                documents = documents.Where(d => d.SubjectId == subjectId.Value);

            if (status.HasValue)
                documents = documents.Where(d => d.Status == status.Value);

            // Populate dropdowns cho filter
            var subjects = await _subjectService.GetAllSubjectsAsync(includeDeleted: false);
            ViewBag.Subjects = new SelectList(subjects, "Id", "SubjectCode", subjectId);
            ViewBag.FilterSubjectId = subjectId;
            ViewBag.FilterStatus = status;
            ViewBag.CanUploadDocuments = false;
            ViewBag.ManagedSubjectIds = new List<int>();

            if (User.IsInRole("Lecturer") && userId.HasValue)
            {
                var assignedSubjects = await _subjectService.GetSubjectsByLecturerIdAsync(userId.Value, includeDeleted: false);
                var managedSubjectIds = assignedSubjects.Select(s => s.Id).ToList();
                ViewBag.ManagedSubjectIds = managedSubjectIds;
                ViewBag.CanUploadDocuments = managedSubjectIds.Any();
            }
            else if (User.IsInRole("Admin"))
            {
                ViewBag.ManagedSubjectIds = subjects.Select(s => s.Id).ToList();
                ViewBag.CanUploadDocuments = false;
            }

            var viewModel = new DocumentListViewModel
            {
                Documents = documents,
                FilterSubjectId = subjectId,
                FilterStatus = status
            };

            return View(viewModel);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: /Document/Upload
        // Form upload tài liệu mới
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Upload()
        {
            await PopulateDropdowns();
            return View(new DocumentUploadViewModel());
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST: /Document/Upload
        // Xử lý upload file thực tế
        // ─────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Upload(DocumentUploadViewModel viewModel)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Không thể xác định người dùng hiện tại.";
                return RedirectToAction(nameof(Index));
            }

            if (!await CanManageSubjectDocumentsAsync(viewModel.SubjectId, userId.Value))
            {
                ModelState.AddModelError(nameof(viewModel.SubjectId), "Bạn chỉ có thể tải tài liệu lên môn học được phân công.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(viewModel.SubjectId);
                return View(viewModel);
            }

            var wwwrootPath = _webHostEnvironment.WebRootPath;
            var (success, message, document) = await _documentService.UploadDocumentAsync(
                viewModel, userId.Value, wwwrootPath);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                await PopulateDropdowns(viewModel.SubjectId);
                return View(viewModel);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST: /Document/UploadChunk
        // Xử lý chunk upload cho Lecturer
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<IActionResult> UploadChunk(
            Microsoft.AspNetCore.Http.IFormFile file, int chunkIndex, int totalChunks, string fileName, string fileGuid,
            string title, int subjectId, int? chapterId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Không thể xác định người dùng hiện tại." });
            }

            if (!await CanManageSubjectDocumentsAsync(subjectId, userId.Value))
            {
                return Json(new { success = false, message = "Bạn chỉ có thể tải tài liệu lên môn học được phân công." });
            }

            var wwwrootPath = _webHostEnvironment.WebRootPath;
            var (success, message, document) = await _documentService.ProcessChunkAsync(
                file, chunkIndex, totalChunks, fileName, fileGuid, title, subjectId, chapterId, userId.Value, wwwrootPath);

            return Json(new { success, message, document });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: /Document/Edit/{id}
        // Form chỉnh sửa tiêu đề và chapter của tài liệu
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài liệu.";
                return RedirectToAction(nameof(Index));
            }

            if (!await CanManageDocumentAsync(id))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa tài liệu này. Chỉ có thể chỉnh sửa tài liệu thuộc môn học được phân công.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new DocumentEditViewModel
            {
                Id = document.Id,
                Title = document.Title,
                SubjectId = document.SubjectId,
                ChapterId = document.ChapterId,
                FileName = document.FileName,
                FileType = document.FileType,
                FileSizeDisplay = document.FileSizeDisplay
            };

            await PopulateEditDropdowns(document.SubjectId, document.ChapterId);
            return View(viewModel);
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST: /Document/Edit/{id}
        // Lưu thay đổi tiêu đề và chapter
        // ─────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecturer,Admin")]
        public async Task<IActionResult> Edit(int id, DocumentEditViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                TempData["ErrorMessage"] = "Dữ liệu không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            if (!await CanManageDocumentAsync(id))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền chỉnh sửa tài liệu này.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                await PopulateEditDropdowns(viewModel.SubjectId, viewModel.ChapterId);
                return View(viewModel);
            }

            var (success, message, document) = await _documentService.UpdateDocumentAsync(viewModel);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                await PopulateEditDropdowns(viewModel.SubjectId, viewModel.ChapterId);
                return View(viewModel);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Details), new { id });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: /Document/Details/{id}
        // Chi tiết tài liệu + form cập nhật trạng thái
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài liệu.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new DocumentDetailViewModel
            {
                Document = document,
                NewStatus = document.Status
            };

            var userId = GetCurrentUserId();
            ViewBag.CanManageDocument = userId.HasValue && await CanManageSubjectDocumentsAsync(document.SubjectId, userId.Value);

            return View(viewModel);
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST: /Document/UpdateStatus/{id}
        // Cập nhật trạng thái Pending / Indexed / Failed
        // ─────────────────────────────────────────────────────────────────────

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, DocumentStatus newStatus)
        {
            if (!await CanManageDocumentAsync(id))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền cập nhật tài liệu này.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var (success, message) = await _documentService.UpdateDocumentStatusAsync(id, newStatus);

            if (success)
                TempData["SuccessMessage"] = message;
            else
                TempData["ErrorMessage"] = message;

            return RedirectToAction(nameof(Details), new { id });
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: /Document/Delete/{id}
        // Trang xác nhận xoá tài liệu
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài liệu.";
                return RedirectToAction(nameof(Index));
            }

            if (!await CanManageDocumentAsync(id))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa tài liệu này.";
                return RedirectToAction(nameof(Index));
            }

            return View(document);
        }

        // ─────────────────────────────────────────────────────────────────────
        // POST: /Document/Delete/{id}
        // Xác nhận xoá: soft delete DB + xoá file vật lý
        // ─────────────────────────────────────────────────────────────────────

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!await CanManageDocumentAsync(id))
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xóa tài liệu này.";
                return RedirectToAction(nameof(Index));
            }

            var wwwrootPath = _webHostEnvironment.WebRootPath;
            var (success, message) = await _documentService.DeleteDocumentAsync(id, wwwrootPath);

            if (success)
                TempData["SuccessMessage"] = message;
            else
                TempData["ErrorMessage"] = message;

            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: /Document/Download/{id}
        // Tải file về trực tiếp
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài liệu.";
                return RedirectToAction(nameof(Index));
            }

            var wwwrootPath = _webHostEnvironment.WebRootPath;
            var fullPath = Path.Combine(wwwrootPath, document.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!System.IO.File.Exists(fullPath))
            {
                TempData["ErrorMessage"] = "File không tồn tại trên hệ thống.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var contentType = document.FileType switch
            {
                "pdf"  => "application/pdf",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                _      => "application/octet-stream"
            };

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(fileBytes, contentType, document.FileName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // AJAX: /Document/GetChaptersBySubject?subjectId={id}
        // Lấy chapters theo subject để load dynamic dropdown
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetChaptersBySubject(int subjectId)
        {
            var chapters = await _chapterService.GetChaptersBySubjectIdAsync(subjectId, includeDeleted: false);
            var result = chapters.Select(c => new { id = c.Id, title = $"Chapter {c.ChapterNumber}: {c.ChapterTitle}" });
            return Json(result);
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        private int? GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                return userId;
            return null;
        }

        private async Task PopulateDropdowns(int? selectedSubjectId = null, int? selectedChapterId = null)
        {
            IEnumerable<SubjectDto> subjects;
            var userId = GetCurrentUserId();

            if (User.IsInRole("Lecturer") && userId.HasValue)
            {
                subjects = await _subjectService.GetSubjectsByLecturerIdAsync(userId.Value, includeDeleted: false);
            }
            else
            {
                subjects = await _subjectService.GetAllSubjectsAsync(includeDeleted: false);
            }

            ViewBag.SubjectList = new SelectList(subjects, "Id", "SubjectCode", selectedSubjectId);

            // Load chapters cho subject đã chọn (nếu có)
            if (selectedSubjectId.HasValue)
            {
                var chapters = await _chapterService.GetChaptersBySubjectIdAsync(selectedSubjectId.Value, includeDeleted: false);
                var chapterItems = chapters.Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"Chapter {c.ChapterNumber}: {c.ChapterTitle}",
                    Selected = c.Id == selectedChapterId
                });
                ViewBag.ChapterList = new SelectList(chapterItems, "Value", "Text", selectedChapterId);
            }
            else
            {
                ViewBag.ChapterList = new SelectList(Enumerable.Empty<SelectListItem>(), "Value", "Text");
            }
        }

        private async Task<bool> CanManageDocumentAsync(int documentId)
        {
            var document = await _documentService.GetDocumentByIdAsync(documentId);
            var userId = GetCurrentUserId();
            return document != null && userId.HasValue && await CanManageSubjectDocumentsAsync(document.SubjectId, userId.Value);
        }

        private async Task<bool> CanManageSubjectDocumentsAsync(int subjectId, int userId)
        {
            if (User.IsInRole("Admin"))
            {
                return true;
            }

            return User.IsInRole("Lecturer")
                && await _subjectService.IsLecturerAssignedToSubjectAsync(subjectId, userId);
        }

        private async Task PopulateEditDropdowns(int selectedSubjectId, int? selectedChapterId = null)
        {
            var chapters = await _chapterService.GetChaptersBySubjectIdAsync(selectedSubjectId, includeDeleted: false);
            var chapterItems = chapters.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"Chapter {c.ChapterNumber}: {c.ChapterTitle}",
                Selected = c.Id == selectedChapterId
            });
            ViewBag.ChapterList = new SelectList(chapterItems, "Value", "Text", selectedChapterId);
        }
    }
}
