using BussinessLayer;
using Microsoft.AspNetCore.Authentication.Cookies;

// --- LOAD ENVIRONMENT VARIABLES FROM .ENV ---
var currentDir = Directory.GetCurrentDirectory();
string? envFilePath = null;
var tempDir = currentDir;
while (!string.IsNullOrEmpty(tempDir))
{
    var path = Path.Combine(tempDir, ".env");
    if (File.Exists(path))
    {
        envFilePath = path;
        break;
    }
    tempDir = Directory.GetParent(tempDir)?.FullName;
}

if (envFilePath == null)
{
    var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
    if (File.Exists(path))
    {
        envFilePath = path;
    }
}

if (envFilePath != null)
{
    foreach (var line in File.ReadAllLines(envFilePath))
    {
        var trimmed = line.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
        
        var parts = trimmed.Split('=', 2);
        if (parts.Length != 2) continue;
        
        var key = parts[0].Trim();
        var val = parts[1].Trim().Trim('"', '\'');
        
        Environment.SetEnvironmentVariable(key, val);
    }
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Register IHttpContextAccessor for accessing HttpContext in controllers and services
builder.Services.AddHttpContextAccessor();

// --- 1 & 2. ĐĂNG KÝ CÁC DỊCH VỤ TẦNG NGHIỆP VỤ & DỮ LIỆU ---
// Gọi phương thức mở rộng từ BussinessLayer để tự đăng ký DbContext, Repositories và Services
// Điều này giúp tầng PresentationLayer KHÔNG cần tham chiếu trực tiếp tới DataAccessLayer
builder.Services.AddBusinessAndDataServices(builder.Configuration);

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

// Lệnh cập nhật Database bằng EF Core Migration (chạy tại thư mục gốc D:\Assignment1):
// dotnet ef database update --project DataAccessLayer --startup-project PresentationLayer
