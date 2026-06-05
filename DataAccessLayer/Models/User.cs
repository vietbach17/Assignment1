using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models
{
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
        [MaxLength(150)]
        public string Email { get; set; } = null!;

        [Required]
        public int RoleId { get; set; }

        /// <summary>Soft delete — tài khoản bị xóa mềm, không thể đăng nhập</summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>Ban — admin khóa tài khoản, không thể đăng nhập</summary>
        public bool IsBanned { get; set; } = false;

        [ForeignKey(nameof(RoleId))]
        public virtual Role Role { get; set; } = null!;
    }
}
