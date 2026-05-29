using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models
{
    [Table("SubjectLecturers")]
    public class SubjectLecturer
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int SubjectId { get; set; }

        [Required]
        public int LecturerId { get; set; }

        [Required]
        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(SubjectId))]
        public virtual Subject Subject { get; set; } = null!;

        [ForeignKey(nameof(LecturerId))]
        public virtual User Lecturer { get; set; } = null!;
    }
}
