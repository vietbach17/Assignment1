using System.Security.Claims;
using BussinessLayer.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PresentationLayer.Models;

namespace PresentationLayer.Controllers
{
    // Controller điều phối toàn bộ luồng Xác thực (Xác thực Cookie, Claims, Đăng nhập, Đăng ký, Đăng xuất)
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly BussinessLayer.Services.IEmailService _emailService;
        private readonly BussinessLayer.Services.IRoleService _roleService;

        // Dependency Injection tiêm IAuthService, IEmailService & IRoleService
        public AuthController(
            IAuthService authService, 
            BussinessLayer.Services.IEmailService emailService,
            BussinessLayer.Services.IRoleService roleService)
        {
            _authService = authService;
            _emailService = emailService;
            _roleService = roleService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Nếu người dùng đã đăng nhập sẵn, tự động chuyển hướng theo vai trò thay vì hiển thị form login
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectUserByRole();
            }

            var model = new LoginViewModel { ReturnUrl = returnUrl };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Gọi Service để xác thực tài khoản qua Database
            var user = await _authService.LoginAsync(model.Username, model.Password);

            if (user == null)
            {
                // Thêm lỗi ModelError hiển thị lên giao diện nếu đăng nhập thất bại
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
                return View(model);
            }

            // --- CLAIMS & COOKIE AUTHENTICATION SETUP ---
            // Claims là các cặp thuộc tính chứa thông tin về danh tính của người dùng (Ví dụ: Tên, Vai trò, ID)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.RoleName), // Cực kỳ quan trọng để dùng attribute [Authorize(Roles = "...")]
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            // Tạo danh tính người dùng gắn liền với cơ chế xác thực bằng Cookie
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Cấu hình thuộc tính cho Cookie (Thời gian hết hạn, ghi nhớ đăng nhập)
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Ghi nhớ đăng nhập giữa các phiên trình duyệt
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7) // Hết hạn sau 7 ngày
            };

            // Tiến hành ghi nhận Đăng nhập (Sign In) - ASP.NET Core sẽ mã hóa Claims Principal thành Cookie và gửi về Client
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Chuyển hướng người dùng dựa theo vai trò (Role) của DTO vừa đăng nhập để tránh lỗi trễ HttpContext
            return RedirectUserByRole(user.RoleName);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register()
        {
            var roles = await _roleService.GetAllRolesAsync();
            ViewBag.Roles = roles.Where(r => !string.Equals(r.RoleName, "Admin", StringComparison.OrdinalIgnoreCase)).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var roles = await _roleService.GetAllRolesAsync();
                ViewBag.Roles = roles.Where(r => !string.Equals(r.RoleName, "Admin", StringComparison.OrdinalIgnoreCase)).ToList();
                return View(model);
            }

            // Gọi Service để xử lý logic đăng ký và truyền thêm email, roleId
            bool isRegistered = await _authService.RegisterAsync(model.Username, model.Password, model.Email, model.RoleId);

            if (!isRegistered)
            {
                ModelState.AddModelError("Username", "Tên đăng nhập này đã tồn tại trên hệ thống.");
                var roles = await _roleService.GetAllRolesAsync();
                ViewBag.Roles = roles.Where(r => !string.Equals(r.RoleName, "Admin", StringComparison.OrdinalIgnoreCase)).ToList();
                return View(model);
            }

            // Gửi email thông báo cho User mới
            var targetRole = await _roleService.GetRoleByIdAsync(model.RoleId);
            var roleName = targetRole?.RoleName ?? "Thành viên";
            var emailSubject = $"Tài khoản {roleName} StudyMind của bạn đã được tạo!";
            var emailBody = $@"
                <html>
                <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <h2 style='color: #1e3b31;'>Chào mừng bạn đến với StudyMind!</h2>
                    <p>Tài khoản {roleName} của bạn đã được khởi tạo thành công bởi Quản trị viên.</p>
                    <p>Dưới đây là thông tin tài khoản đăng nhập của bạn:</p>
                    <table style='border-collapse: collapse; width: 100%; max-width: 400px;'>
                        <tr style='background-color: #f4f6f8;'>
                            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Tên đăng nhập:</td>
                            <td style='padding: 10px; border: 1px solid #ddd;'>{model.Username}</td>
                        </tr>
                        <tr>
                            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Mật khẩu:</td>
                            <td style='padding: 10px; border: 1px solid #ddd;'>{model.Password}</td>
                        </tr>
                        <tr style='background-color: #f4f6f8;'>
                            <td style='padding: 10px; border: 1px solid #ddd; font-weight: bold;'>Vai trò:</td>
                            <td style='padding: 10px; border: 1px solid #ddd;'>{roleName}</td>
                        </tr>
                    </table>
                    <p>Vui lòng đăng nhập hệ thống và đổi mật khẩu để bảo mật tài khoản.</p>
                    <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'/>
                    <p style='font-size: 12px; color: #777;'>Đây là email tự động từ hệ thống StudyMind, vui lòng không trả lời trực tiếp email này.</p>
                </body>
                </html>";

            await _emailService.SendEmailAsync(model.Email, emailSubject, emailBody);

            TempData["SuccessMessage"] = $"Đã cấp tài khoản thành công cho {roleName} {model.Username} và gửi email thông báo.";
            return RedirectToAction("Users", "Admin"); // Chuyển về danh sách User của Admin
        }

        // Action loại bỏ Cookie và Đăng xuất phiên làm việc của người dùng
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            // Thực hiện Sign Out loại bỏ session Cookie khỏi trình duyệt
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }

        // Màn hình thông báo từ chối truy cập khi phân quyền thất bại
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Hàm helper điều hướng chuyển trang dựa trên Role của người dùng (nhận trực tiếp hoặc trích xuất từ Claims)
        private IActionResult RedirectUserByRole(string? roleName = null)
        {
            // Trường hợp 1: Có truyền trực tiếp RoleName (Ví dụ: Vừa đăng nhập thành công ở POST Login)
            if (!string.IsNullOrEmpty(roleName))
            {
                return RedirectToAction("Index", "Home");
                //if (string.Equals(roleName, "Admin", StringComparison.OrdinalIgnoreCase))
                //{
                //    return RedirectToAction("Dashboard", "Admin");
                //}
                //if (string.Equals(roleName, "Lecturer", StringComparison.OrdinalIgnoreCase))
                //{
                //    return RedirectToAction("Index", "Document");
                //}
                //if (string.Equals(roleName, "Student", StringComparison.OrdinalIgnoreCase))
                //{
                //    return RedirectToAction("Chat", "Home");
                //}
            }

            // Trường hợp 2: Không truyền RoleName (Ví dụ: Đã có sẵn Cookie ở GET Login)
            //if (User.IsInRole("Admin"))
            //{
            //    return RedirectToAction("Dashboard", "Admin");
            //}
            //if (User.IsInRole("Lecturer"))
            //{
            //    return RedirectToAction("Index", "Document");
            //}
            //if (User.IsInRole("Student"))
            //{
            //    // Học viên -> Phòng chat
            //    return RedirectToAction("Chat", "Home");
            //}

            //return RedirectToAction("Chat", "Home"); // Mặc định chuyển về trang Home Student

            return RedirectToAction("Index", "Home");
        }
    }
}
