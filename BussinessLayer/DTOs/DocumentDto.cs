using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BussinessLayer.DTOs
{
    // ─────────────────────────────────────────────────────────────────────────
    // DTO (Read) — Dùng để truyền dữ liệu Document từ Service lên Controller/View
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// DTO chứa thông tin đầy đủ của một tài liệu (dùng cho list và detail)
    /// </summary>
    public class DocumentDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string FileName { get; set; } = null!;

        public string StoredFileName { get; set; } = null!;

        public string FilePath { get; set; } = null!;

        public long FileSize { get; set; }

        public string FileType { get; set; } = null!;

        public DocumentStatus Status { get; set; }

        public string StatusDisplayName => Status switch
        {
            DocumentStatus.Pending  => "Pending",
            DocumentStatus.Indexed  => "Indexed",
            DocumentStatus.Failed   => "Failed",
            _                       => "Unknown"
        };

        public int SubjectId { get; set; }

        public string? SubjectCode { get; set; }

        public string? SubjectName { get; set; }

        public int? ChapterId { get; set; }

        public string? ChapterTitle { get; set; }

        public int UploadedByUserId { get; set; }

        public string? UploadedByUsername { get; set; }

        public DateTime UploadedDate { get; set; }

        public bool IsDeleted { get; set; }

        /// <summary>Định dạng FileSize thành chuỗi dễ đọc (KB, MB)</summary>
        public string FileSizeDisplay
        {
            get
            {
                if (FileSize < 1024) return $"{FileSize} B";
                if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
                return $"{FileSize / (1024.0 * 1024.0):F1} MB";
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ViewModel — Upload form
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ViewModel cho form upload tài liệu mới
    /// </summary>
    public class DocumentUploadViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề tài liệu")]
        [MaxLength(300, ErrorMessage = "Tiêu đề không được vượt quá 300 ký tự")]
        [Display(Name = "Tiêu đề tài liệu")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng chọn môn học")]
        [Display(Name = "Môn học")]
        public int SubjectId { get; set; }

        [Display(Name = "Chương (tuỳ chọn)")]
        public int? ChapterId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn file để tải lên")]
        [Display(Name = "Tài liệu (PDF / DOCX / PPTX)")]
        public IFormFile File { get; set; } = null!;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ViewModel — Document list
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ViewModel cho trang danh sách tài liệu (có thể filter theo Subject)
    /// </summary>
    public class DocumentListViewModel
    {
        public IEnumerable<DocumentDto> Documents { get; set; } = new List<DocumentDto>();

        /// <summary>Filter: SubjectId đang được chọn (null = tất cả)</summary>
        public int? FilterSubjectId { get; set; }

        /// <summary>Filter: Trạng thái đang được chọn (null = tất cả)</summary>
        public DocumentStatus? FilterStatus { get; set; }

        /// <summary>Tổng số tài liệu hiển thị</summary>
        public int TotalCount => Documents.Count();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ViewModel — Document detail
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// ViewModel cho trang chi tiết tài liệu (kèm form cập nhật trạng thái)
    /// </summary>
    public class DocumentDetailViewModel
    {
        public DocumentDto Document { get; set; } = null!;

        /// <summary>Trạng thái mới sẽ được set (dùng trong form UpdateStatus)</summary>
        public DocumentStatus NewStatus { get; set; }
    }
}
