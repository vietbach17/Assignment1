using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models
{
    /// <summary>
    /// Trạng thái xử lý của tài liệu
    /// Pending: Mới upload, chưa xử lý
    /// Indexed: Đã được AI index thành công
    /// Failed: Quá trình xử lý thất bại
    /// </summary>
    /// update 3 status cho gv
    public enum DocumentStatus
    {
        Pending = 0,
        Indexed = 1,
        Failed = 2
    }

    [Table("Documents")]
    public class Document
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Tên hiển thị do người dùng nhập</summary>
        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = null!;

        /// <summary>Tên file gốc khi upload (ví dụ: slide_01.pptx)</summary>
        [Required]
        [MaxLength(500)]
        public string FileName { get; set; } = null!;

        /// <summary>Tên file lưu trên disk (GUID để tránh trùng lặp, ví dụ: a1b2c3d4.pptx)</summary>
        [Required]
        [MaxLength(500)]
        public string StoredFileName { get; set; } = null!;

        /// <summary>Đường dẫn tương đối từ wwwroot (ví dụ: uploads/a1b2c3d4.pptx)</summary>
        [Required]
        [MaxLength(1000)]
        public string FilePath { get; set; } = null!;

        /// <summary>Dung lượng file tính bằng bytes</summary>
        public long FileSize { get; set; }

        /// <summary>Loại file: "pdf", "docx", "pptx"</summary>
        [Required]
        [MaxLength(10)]
        public string FileType { get; set; } = null!;

        /// <summary>Mã băm SHA-256 nội dung file để tránh upload trùng lặp</summary>
        [Required]
        [MaxLength(64)]
        public string FileHash { get; set; } = null!;

        /// <summary>Trạng thái xử lý tài liệu: Pending / Indexed / Failed</summary>
        [Required]
        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;

        // Liên kết Subject (bắt buộc)
        [Required]
        public int SubjectId { get; set; }

        // Liên kết Chapter (tuỳ chọn — tài liệu có thể thuộc về 1 Chapter cụ thể)
        public int? ChapterId { get; set; }

        // Người upload
        [Required]
        public int UploadedByUserId { get; set; }

        [Required]
        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        // Soft delete support
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        [ForeignKey(nameof(SubjectId))]
        public virtual Subject Subject { get; set; } = null!;

        [ForeignKey(nameof(ChapterId))]
        public virtual Chapter? Chapter { get; set; }

        [ForeignKey(nameof(UploadedByUserId))]
        public virtual User UploadedBy { get; set; } = null!;
    }
}
