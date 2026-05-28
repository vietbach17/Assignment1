using Microsoft.EntityFrameworkCore;
using DataAccessLayer.Models;
using BCryptNet = BCrypt.Net.BCrypt;

namespace DataAccessLayer.DbContexts
{
    // Lớp DbContext quản lý việc kết nối và thao tác dữ liệu với PostgreSQL
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Đảm bảo Username là duy nhất (Unique) để tối ưu tìm kiếm và đăng ký
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // Cấu hình mối quan hệ 1 - Nhiều giữa User và Role
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict); // Tránh xóa cascade khi xóa Role

            // Seed Data cho 3 nhóm quyền cốt lõi
            var adminRole = new Role { Id = 1, RoleName = "Admin" };
            var lecturerRole = new Role { Id = 2, RoleName = "Lecturer" };
            var studentRole = new Role { Id = 3, RoleName = "Student" };

            modelBuilder.Entity<Role>().HasData(adminRole, lecturerRole, studentRole);

            // Seed Data cho 3 Users mẫu ứng với 3 Roles (mật khẩu băm bằng BCrypt)
            // LƯU Ý: Ở dự án thực tế, mật khẩu seed nên được lưu trữ cấu hình bảo mật hoặc dùng biến môi trường.
            var adminUser = new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = BCryptNet.HashPassword("admin123"), // Mật khẩu gốc: admin123
                RoleId = 1
            };

            var lecturerUser = new User
            {
                Id = 2,
                Username = "lecturer",
                PasswordHash = BCryptNet.HashPassword("lecturer123"), // Mật khẩu gốc: lecturer123
                RoleId = 2
            };

            var studentUser = new User
            {
                Id = 3,
                Username = "student",
                PasswordHash = BCryptNet.HashPassword("student123"), // Mật khẩu gốc: student123
                RoleId = 3
            };

            modelBuilder.Entity<User>().HasData(adminUser, lecturerUser, studentUser);
        }
    }
}
