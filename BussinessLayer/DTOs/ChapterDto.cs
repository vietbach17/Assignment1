using System.ComponentModel.DataAnnotations;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO để truyền dữ liệu Chapter giữa các lớp
    /// </summary>
    public class ChapterDto
    {
        public int Id { get; set; }
        
        public int ChapterNumber { get; set; }
        
        public string ChapterTitle { get; set; } = null!;
        
        public string? Description { get; set; }
        
        public int SubjectId { get; set; }
        
        public string? SubjectCode { get; set; }
        
        public string? SubjectName { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public string? CreatedByUsername { get; set; }
        
        public DateTime? UpdatedDate { get; set; }
        
        public string? UpdatedByUsername { get; set; }
        
        public bool IsDeleted { get; set; }
    }

    /// <summary>
    /// DTO để tạo Chapter mới với validation
    /// </summary>
    public class CreateChapterDto
    {
        [Required(ErrorMessage = "Chapter Number is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Chapter Number must be a positive integer")]
        public int ChapterNumber { get; set; }

        [Required(ErrorMessage = "Chapter Title is required")]
        [MaxLength(200, ErrorMessage = "Chapter Title cannot exceed 200 characters")]
        public string ChapterTitle { get; set; } = null!;

        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        public int SubjectId { get; set; }
    }

    /// <summary>
    /// DTO để cập nhật Chapter với validation
    /// </summary>
    public class UpdateChapterDto
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Chapter Number is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Chapter Number must be a positive integer")]
        public int ChapterNumber { get; set; }

        [Required(ErrorMessage = "Chapter Title is required")]
        [MaxLength(200, ErrorMessage = "Chapter Title cannot exceed 200 characters")]
        public string ChapterTitle { get; set; } = null!;

        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        public int SubjectId { get; set; }
    }
}
