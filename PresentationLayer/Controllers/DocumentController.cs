using System.Security.Claims;
using BussinessLayer.DTOs;
using BussinessLayer.Services;
using DataAccessLayer.Models;
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

            if (subjectId.HasValue)
                documents = await _documentService.GetDocumentsBySubjectAsync(subjectId.Value);
            else
                documents = await _documentService.GetAllDocumentsAsync(includeDeleted: false);

            // Filter theo trạng thái nếu có
            if (status.HasValue)
                documents = documents.Where(d => d.Status == status.Value);

            // Populate dropdowns cho filter
            var subjects = await _subjectService.GetAllSubjectsAsync(includeDeleted: false);
            ViewBag.Subjects = new SelectList(subjects, "Id", "SubjectCode", subjectId);
            ViewBag.FilterSubjectId = subjectId;
            ViewBag.FilterStatus = status;

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
        public async Task<IActionResult> Upload(DocumentUploadViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(viewModel.SubjectId);
                return View(viewModel);
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                TempData["ErrorMessage"] = "Không thể xác định người dùng hiện tại.";
                return RedirectToAction(nameof(Index));
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
            var subjects = await _subjectService.GetAllSubjectsAsync(includeDeleted: false);
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
    }
}
