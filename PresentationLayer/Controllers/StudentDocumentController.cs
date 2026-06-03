using BussinessLayer.Services;
using DataAccessLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Claims;

namespace PresentationLayer.Controllers
{
    /// <summary>
    /// Controller cho phép Student xem và tải về tài liệu
    /// Chỉ Student có quyền truy cập
    /// </summary>
    [Authorize(Roles = "Student")]
    public class StudentDocumentController : Controller
    {
        private readonly ISubjectService _subjectService;
        private readonly IChapterService _chapterService;
        private readonly IDocumentService _documentService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public StudentDocumentController(
            ISubjectService subjectService,
            IChapterService chapterService,
            IDocumentService documentService,
            IWebHostEnvironment webHostEnvironment,
            IHttpContextAccessor httpContextAccessor)
        {
            _subjectService = subjectService;
            _chapterService = chapterService;
            _documentService = documentService;
            _webHostEnvironment = webHostEnvironment;
            _httpContextAccessor = httpContextAccessor;
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: /StudentDocument/Subjects
        // Hiển thị danh sách tất cả môn học
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Subjects()
        {
            var subjects = await _subjectService.GetAllSubjectsAsync(includeDeleted: false);
            return View(subjects);
        }

        [HttpGet]
        public async Task<IActionResult> AllDocuments()
        {
            var allDocuments = await _documentService.GetAllDocumentsAsync();
            return View(allDocuments);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: /StudentDocument/Chapters?subjectId={id}
        // Hiển thị danh sách chapters của một môn học
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Chapters(int id)
        {
            var subject = await _subjectService.GetSubjectByIdAsync(id, includeDeleted: false);
            if (subject == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy môn học.";
                return RedirectToAction(nameof(Subjects));
            }

            var chapters = await _chapterService.GetChaptersBySubjectIdAsync(id, includeDeleted: false);

            ViewBag.SubjectId = subject.Id;
            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectName = subject.SubjectName;
            ViewBag.SubjectDescription = subject.Description;

            return View(chapters);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: /StudentDocument/Documents?chapterId={id}
        // Hiển thị danh sách tài liệu trong một chapter
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Documents()
        {
            var allDocuments = await _documentService.GetAllDocumentsAsync();
            return View(allDocuments);
        }
        [HttpGet]
        public async Task<IActionResult> DocumentInChapter(int chapterId)
        {
            TempData["ErrorMessage"] = null;
            var chapter = await _chapterService.GetChapterWithSubjectAsync(chapterId, includeDeleted: false);
            if (chapter == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy chapter.";
                return RedirectToAction(nameof(Documents));
            }

            // Lấy tất cả tài liệu của subject
            var allDocuments = await _documentService.GetDocumentsBySubjectAsync(chapter.SubjectId);
            
            // Lọc tài liệu thuộc chapter này và chỉ lấy tài liệu đã được Indexed
            var documents = allDocuments
                .Where(d => d.ChapterId == chapterId && d.Status == BussinessLayer.DTOs.DocumentStatus.Indexed)
                .OrderBy(d => d.UploadedDate);

            ViewBag.ChapterId = chapter.Id;
            ViewBag.ChapterNumber = chapter.ChapterNumber;
            ViewBag.ChapterTitle = chapter.ChapterTitle;
            ViewBag.ChapterDescription = chapter.Description;
            ViewBag.SubjectId = chapter.SubjectId;
            ViewBag.SubjectCode = chapter.SubjectCode;
            ViewBag.SubjectName = chapter.SubjectName;

            return View(documents);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: /StudentDocument/ViewDocument/{id}
        // Xem chi tiết tài liệu
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> ViewDocument(int id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null || document.Status != BussinessLayer.DTOs.DocumentStatus.Indexed)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài liệu hoặc tài liệu chưa sẵn sàng.";
                return RedirectToAction(nameof(Documents));
            }

            return View(document);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: /StudentDocument/ViewFile/{id}
        // Xem file trực tiếp trên trình duyệt (PDF viewer hoặc Office viewer)
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> ViewFile(int id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null || document.Status != BussinessLayer.DTOs.DocumentStatus.Indexed)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài liệu hoặc tài liệu chưa sẵn sàng.";
                return RedirectToAction(nameof(Documents));
               
            }

            // Tạo URL để xem file
            var fileUrl = Url.Content($"~/{document.FilePath}");
            ViewBag.FileUrl = fileUrl;
            ViewBag.FileType = document.FileType;

            return View(document);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: /StudentDocument/Download/{id}
        // Tải tài liệu về máy
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null || document.Status != BussinessLayer.DTOs.DocumentStatus.Indexed)
            {
                TempData["ErrorMessage"] = "Không tìm thấy tài liệu hoặc tài liệu chưa sẵn sàng.";
                return RedirectToAction(nameof(Documents));
            }

            var wwwrootPath = _webHostEnvironment.WebRootPath;
            var fullPath = Path.Combine(wwwrootPath, document.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!System.IO.File.Exists(fullPath))
            {
                TempData["ErrorMessage"] = "File không tồn tại trên hệ thống.";
                return RedirectToAction(nameof(ViewDocument), new { id });
            }

            var contentType = document.FileType switch
            {
                "pdf" => "application/pdf",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                _ => "application/octet-stream"
            };

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            return File(fileBytes, contentType, document.FileName);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: /StudentDocument/PreviewFile/{id}
        // Trả về file để hiển thị inline trong browser (cho PDF)
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> PreviewFile(int id)
        {
            var document = await _documentService.GetDocumentByIdAsync(id);
            if (document == null || document.Status != BussinessLayer.DTOs.DocumentStatus.Indexed)
            {
                return NotFound();
            }

            var wwwrootPath = _webHostEnvironment.WebRootPath;
            var fullPath = Path.Combine(wwwrootPath, document.FilePath.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound();
            }

            var contentType = document.FileType switch
            {
                "pdf" => "application/pdf",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                _ => "application/octet-stream"
            };

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fullPath);
            
            // Trả về file với Content-Disposition: inline để browser hiển thị thay vì download
            return File(fileBytes, contentType);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET: /StudentDocument/SubjectDocuments?subjectId={id}
        // Xem tất cả tài liệu của một môn học (không phân theo chapter)
        // ─────────────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> SubjectDocuments(int subjectId)
        {
            TempData["ErrorMessage"] = null;
            var subject = await _subjectService.GetSubjectByIdAsync(subjectId, includeDeleted: false);
            if (subject == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy môn học.";
                return RedirectToAction(nameof(Subjects));
            }

            // Lấy tất cả tài liệu đã được Indexed của subject
            var allDocuments = await _documentService.GetDocumentsBySubjectAsync(subjectId);
            var documents = allDocuments
                .Where(d => d.Status == BussinessLayer.DTOs.DocumentStatus.Indexed)
                .OrderBy(d => d.ChapterId)
                .ThenBy(d => d.UploadedDate);

            ViewBag.SubjectId = subject.Id;
            ViewBag.SubjectCode = subject.SubjectCode;
            ViewBag.SubjectName = subject.SubjectName;
            ViewBag.SubjectDescription = subject.Description;

            return View(documents);
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
    }
}
