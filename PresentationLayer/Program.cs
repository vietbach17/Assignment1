using DataAccessLayer.DbContexts;
using DataAccessLayer.Repositories;
using BussinessLayer.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// --- 1. ĐĂNG KÝ DBCONTEXT KẾT NỐI POSTGRESQL ---
// EF Core DbContext quản lý kết nối và các truy vấn tới DB PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString, b => b.MigrationsAssembly("DataAccessLayer")));

// --- 2. ĐĂNG KÝ DEPENDENCY INJECTION (DI) ---
// Đăng ký các Repository và Service vào hệ thống DI Container dưới dạng Scoped (Khởi tạo lại trên mỗi Request)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

// --- 3. CẤU HÌNH COOKIE AUTHENTICATION ---
// Đăng ký và cấu hình cơ chế xác thực bằng Cookie (Cookie Authentication)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Đường dẫn đến trang đăng nhập khi người dùng cố truy cập trang bảo mật mà chưa đăng nhập
        options.LoginPath = "/Auth/Login";
        
        // Đường dẫn khi người dùng cố truy cập trang bị giới hạn quyền (không đủ phân vai trò - Role)
        options.AccessDeniedPath = "/Auth/AccessDenied";
        
        // Cấu hình tên Cookie lưu trữ trên trình duyệt
        options.Cookie.Name = "Assignment1.AuthCookie";
        
        // Thời gian sống của Cookie trên trình duyệt
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        
        // Bật tính năng gia hạn tự động thời gian sống khi người dùng hoạt động tích cực
        options.SlidingExpiration = true;
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// --- 4. MIDDLEWARE XÁC THỰC VÀ PHÂN QUYỀN (QUAN TRỌNG VỀ THỨ TỰ) ---
// UseAuthentication() dùng để trích xuất Cookie, giải mã Claims gắn vào HttpContext.User
app.UseAuthentication();

// UseAuthorization() dùng để kiểm tra xem User hiện tại có quyền truy cập dựa trên Role/Claims của họ không
app.UseAuthorization();

// Định nghĩa Router mặc định cho ứng dụng MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
