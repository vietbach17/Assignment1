using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models
{
    // Lớp thực thể đại diện cho Bảng Role (Vai trò) trong Database
    [Table("Roles")]
    public class Role
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string RoleName { get; set; } = null!;

        // Quan hệ 1 - Nhiều: Một Role có thể có nhiều Users
        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
