using System.ComponentModel.DataAnnotations;

namespace BussinessLayer.DTOs
{
    /// <summary>
    /// DTO để truyền dữ liệu Subject giữa các lớp
    /// </summary>
    public class SubjectDto
    {
        public int Id { get; set; }
        
        public string SubjectCode { get; set; } = null!;
        
        public string SubjectName { get; set; } = null!;
        
        public string? Description { get; set; }
        
        public DateTime CreatedDate { get; set; }
        
        public List<ChapterDto>? Chapters { get; set; }
        
        public List<string> AssignedLecturerNames { get; set; } = new List<string>();
    }

    /// <summary>
    /// DTO để tạo Subject mới với validation
    /// </summary>
    public class CreateSubjectDto
    {
        [Required(ErrorMessage = "Subject Code is required")]
        [MaxLength(20, ErrorMessage = "Subject Code cannot exceed 20 characters")]
        public string SubjectCode { get; set; } = null!;

        [Required(ErrorMessage = "Subject Name is required")]
        [MaxLength(200, ErrorMessage = "Subject Name cannot exceed 200 characters")]
        public string SubjectName { get; set; } = null!;

        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// DTO để cập nhật Subject với validation
    /// </summary>
    public class UpdateSubjectDto
    {
        [Required]
        public int Id { get; set; }

        [Required(ErrorMessage = "Subject Code is required")]
        [MaxLength(20, ErrorMessage = "Subject Code cannot exceed 20 characters")]
        public string SubjectCode { get; set; } = null!;

        [Required(ErrorMessage = "Subject Name is required")]
        [MaxLength(200, ErrorMessage = "Subject Name cannot exceed 200 characters")]
        public string SubjectName { get; set; } = null!;

        [MaxLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }
    }
}
