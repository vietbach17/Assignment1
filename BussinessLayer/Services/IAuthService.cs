using DataAccessLayer.Models;

namespace BussinessLayer.Services
{
    // Giao diện Service cung cấp các tính năng xác thực cốt lõi
    public interface IAuthService
    {
        // Xử lý đăng nhập: Kiểm tra username và password, trả về thông tin User kèm Role nếu hợp lệ
        Task<User?> LoginAsync(string username, string password);

        // Xử lý đăng ký: Tạo tài khoản mới với vai trò Student mặc định, băm mật khẩu bằng BCrypt
        Task<bool> RegisterAsync(string username, string password);
    }
}
