using DataAccessLayer.DbContexts;
using DataAccessLayer.Repositories;
using BussinessLayer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BussinessLayer
{
    // Lớp chứa phương thức mở rộng (Extension Method) để tự đăng ký các dịch vụ Dependency Injection
    // Việc này giúp PresentationLayer không cần biết bất kỳ chi tiết nào về DataAccessLayer (EF Core, PostgreSQL, Repositories)
    public static class DependencyInjection
    {
        public static IServiceCollection AddBusinessAndDataServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Đăng ký DbContext kết nối tới PostgreSQL (DataAccessLayer)
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString, b => b.MigrationsAssembly("DataAccessLayer")));

            // 2. Đăng ký Repositories thuộc tầng DataAccessLayer (DAL)
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRoleRepository, RoleRepository>();
            services.AddScoped<ISubjectRepository, SubjectRepository>();
            services.AddScoped<IChapterRepository, ChapterRepository>();

            // 3. Đăng ký Services thuộc tầng BussinessLayer (BLL)
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddScoped<IChapterService, ChapterService>();

            return services;
        }
    }
}
