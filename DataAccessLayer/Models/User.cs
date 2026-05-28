using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models
{
    // Lớp thực thể đại diện cho Bảng User (Người dùng) trong Database
    [Table("Users")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = null!;

        [Required]
        public int RoleId { get; set; }

        // Khóa ngoại liên kết tới thực thể Role
        [ForeignKey(nameof(RoleId))]
        public virtual Role Role { get; set; } = null!;
    }
}
