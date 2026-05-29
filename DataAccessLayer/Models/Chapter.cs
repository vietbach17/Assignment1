using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models
{
    [Table("Chapters")]
    public class Chapter
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ChapterNumber must be a positive integer")]
        public int ChapterNumber { get; set; }

        [Required]
        [MaxLength(200)]
        public string ChapterTitle { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public int CreatedByUserId { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public int? UpdatedByUserId { get; set; }

        // Soft delete support
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        [ForeignKey(nameof(SubjectId))]
        public virtual Subject Subject { get; set; } = null!;

        [ForeignKey(nameof(CreatedByUserId))]
        public virtual User CreatedBy { get; set; } = null!;

        [ForeignKey(nameof(UpdatedByUserId))]
        public virtual User? UpdatedBy { get; set; }
    }
}
