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
        public DbSet<Subject> Subjects { get; set; } = null!;
        public DbSet<Chapter> Chapters { get; set; } = null!;
        public DbSet<SubjectLecturer> SubjectLecturers { get; set; } = null!;

        // ============ SUBCRIPTIONS (GÓI HỘI VIÊN) ============
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } = null!;
        public DbSet<StudentSubscription> StudentSubscriptions { get; set; } = null!;
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; } = null!;

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

            // Seed Data cho 3 Users mẫu ứng với 3 Roles (mật khẩu băm tĩnh bằng BCrypt để tránh trôi migrations)
            // LƯU Ý: Ở dự án thực tế, mật khẩu seed nên được lưu trữ cấu hình bảo mật hoặc dùng biến môi trường.
            var adminUser = new User
            {
                Id = 1,
                Username = "admin",
                PasswordHash = "$2a$11$p7e/dWqp3/H5V2hA8gfj4egjrUGPAPfbZqBqMcSvnBcc/Qc8qpjcq", // Mật khẩu gốc: admin123
                RoleId = 1
            };

            var lecturerUser = new User
            {
                Id = 2,
                Username = "lecturer",
                PasswordHash = "$2a$11$.V0CPaW.aVn4ajd6qwur7eF84ysDgtwM6iTNNiVTUaC77F2nMaNji", // Mật khẩu gốc: lecturer123
                RoleId = 2
            };

            var studentUser = new User
            {
                Id = 3,
                Username = "student",
                PasswordHash = "$2a$11$9Eg5STUA/KUfGzB3ubcC0OGv7Mph4h14Lj3lSBPgznmpJ4Sh73oAi", // Mật khẩu gốc: student123
                RoleId = 3
            };

            modelBuilder.Entity<User>().HasData(adminUser, lecturerUser, studentUser);

            // Subject configuration
            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.SubjectCode)
                .IsUnique();

            modelBuilder.Entity<Subject>()
                .HasMany(s => s.Chapters)
                .WithOne(c => c.Subject)
                .HasForeignKey(c => c.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Subject>()
                .HasMany(s => s.SubjectLecturers)
                .WithOne(sl => sl.Subject)
                .HasForeignKey(sl => sl.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Subject>()
                .HasOne(s => s.CreatedBy)
                .WithMany()
                .HasForeignKey(s => s.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Subject>()
                .HasOne(s => s.UpdatedBy)
                .WithMany()
                .HasForeignKey(s => s.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Chapter configuration
            modelBuilder.Entity<Chapter>()
                .HasIndex(c => new { c.SubjectId, c.ChapterNumber })
                .IsUnique();

            modelBuilder.Entity<Chapter>()
                .HasOne(c => c.CreatedBy)
                .WithMany()
                .HasForeignKey(c => c.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Chapter>()
                .HasOne(c => c.UpdatedBy)
                .WithMany()
                .HasForeignKey(c => c.UpdatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // SubjectLecturer configuration
            modelBuilder.Entity<SubjectLecturer>()
                .HasIndex(sl => new { sl.SubjectId, sl.LecturerId })
                .IsUnique();

            modelBuilder.Entity<SubjectLecturer>()
                .HasOne(sl => sl.Lecturer)
                .WithMany()
                .HasForeignKey(sl => sl.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed data
            SeedSubjectsAndChapters(modelBuilder);


            // ============ SUBCRIPTION ============
            // 1. Cấu hình quan hệ giữa StudentSubscription và User (1 Student - 1 Subscription)
            modelBuilder.Entity<StudentSubscription>()
                .HasOne(s => s.User)
                .WithMany() // Một user có thể có lịch sử gói, hoặc để trống nếu quản lý gói hiện tại
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa tài khoản thì xóa luôn thông tin gói đăng ký

            // 2. Seed Data cho 3 gói dịch vụ Chatbot mặc định
            modelBuilder.Entity<SubscriptionPlan>().HasData(
                new SubscriptionPlan { Id = 1, Name = "Free", Price = 0, QuestionLimit = 5, Description = "Gói mặc định cho sinh viên mới tạo tài khoản." },
                new SubscriptionPlan { Id = 2, Name = "Basic", Price = 50000, QuestionLimit = 20, Description = "Phù hợp nhu cầu ôn thi thông thường." },
                new SubscriptionPlan { Id = 3, Name = "Pro", Price = 150000, QuestionLimit = 9999, Description = "Hỏi đáp không giới hạn." }
            );
        }

        private void SeedSubjectsAndChapters(ModelBuilder modelBuilder)
        {
            var prn222Subject = new Subject
            {
                Id = 1,
                SubjectCode = "PRN222",
                SubjectName = "Advanced Cross-Platform Application Programming With .NET",
                Description = "This course provides knowledge and skills in developing cross-platform applications using .NET technologies.",
                CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                CreatedByUserId = 1, // Created by admin
                IsDeleted = false
            };

            modelBuilder.Entity<Subject>().HasData(prn222Subject);

            var chapters = new[]
            {
                new Chapter
                {
                    Id = 1,
                    ChapterNumber = 1,
                    ChapterTitle = "Networking Programming",
                    Description = "Introduction to network programming concepts and protocols",
                    SubjectId = 1,
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedByUserId = 1,
                    IsDeleted = false
                },
                new Chapter
                {
                    Id = 2,
                    ChapterNumber = 2,
                    ChapterTitle = "Asynchronous and Parallel Programming in .NET",
                    Description = "Understanding async/await patterns and parallel processing",
                    SubjectId = 1,
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedByUserId = 1,
                    IsDeleted = false
                },
                new Chapter
                {
                    Id = 3,
                    ChapterNumber = 3,
                    ChapterTitle = "Dependency Injection in .NET",
                    Description = "Implementing DI patterns and IoC containers in .NET applications",
                    SubjectId = 1,
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedByUserId = 1,
                    IsDeleted = false
                },
                new Chapter
                {
                    Id = 4,
                    ChapterNumber = 4,
                    ChapterTitle = "Building Web Application using ASP.NET Core MVC",
                    Description = "Creating MVC web applications with ASP.NET Core",
                    SubjectId = 1,
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedByUserId = 1,
                    IsDeleted = false
                },
                new Chapter
                {
                    Id = 5,
                    ChapterNumber = 5,
                    ChapterTitle = "Building Websites Using ASP.NET Core Razor Pages",
                    Description = "Developing page-based web applications with Razor Pages",
                    SubjectId = 1,
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedByUserId = 1,
                    IsDeleted = false
                },
                new Chapter
                {
                    Id = 6,
                    ChapterNumber = 6,
                    ChapterTitle = "Building a Web App with Blazor and ASP .Net Core",
                    Description = "Creating interactive web UIs using Blazor framework",
                    SubjectId = 1,
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedByUserId = 1,
                    IsDeleted = false
                },
                new Chapter
                {
                    Id = 7,
                    ChapterNumber = 7,
                    ChapterTitle = "Real-Time Communication",
                    Description = "Implementing real-time features with SignalR",
                    SubjectId = 1,
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedByUserId = 1,
                    IsDeleted = false
                },
                new Chapter
                {
                    Id = 8,
                    ChapterNumber = 8,
                    ChapterTitle = "Background Tasks with Worker Service",
                    Description = "Creating and managing background services in .NET",
                    SubjectId = 1,
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    CreatedByUserId = 1,
                    IsDeleted = false
                }
            };

            modelBuilder.Entity<Chapter>().HasData(chapters);

            // Assign PRN222 to the lecturer user (Id = 2)
            var lecturerAssignment = new SubjectLecturer
            {
                Id = 1,
                SubjectId = 1,
                LecturerId = 2,
                AssignedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            };

            modelBuilder.Entity<SubjectLecturer>().HasData(lecturerAssignment);
        }
    }
}
