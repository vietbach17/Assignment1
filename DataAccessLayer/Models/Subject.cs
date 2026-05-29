using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models
{
    [Table("Subjects")]
    public class Subject
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string SubjectCode { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string SubjectName { get; set; } = null!;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [Required]
        public int CreatedByUserId { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public int? UpdatedByUserId { get; set; }

        // Soft delete support
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        [ForeignKey(nameof(CreatedByUserId))]
        public virtual User CreatedBy { get; set; } = null!;

        [ForeignKey(nameof(UpdatedByUserId))]
        public virtual User? UpdatedBy { get; set; }

        public virtual ICollection<Chapter> Chapters { get; set; } = new List<Chapter>();

        public virtual ICollection<SubjectLecturer> SubjectLecturers { get; set; } = new List<SubjectLecturer>();
    }
}
