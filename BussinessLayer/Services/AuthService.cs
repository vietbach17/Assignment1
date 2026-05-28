using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using BCryptNet = BCrypt.Net.BCrypt;

namespace BussinessLayer.Services
{
    // Lớp dịch vụ thực thi các logic nghiệp vụ xác thực tài khoản
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        // Dependency Injection tiêm Repositories vào Service
        public AuthService(IUserRepository userRepository, IRoleRepository roleRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public async Task<User?> LoginAsync(string username, string password)
        {
            // Tìm kiếm người dùng theo username
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
            {
                return null; // Không tồn tại tên đăng nhập
            }

            // Kiểm tra mật khẩu băm BCrypt.
            // BCrypt.Verify tự động trích xuất Salt từ chuỗi hash và so khớp an toàn
            bool isPasswordValid = BCryptNet.Verify(password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return null; // Mật khẩu không chính xác
            }

            return user; // Đăng nhập thành công, trả về thực thể User (đã bao gồm Role)
        }

        public async Task<bool> RegisterAsync(string username, string password)
        {
            // Kiểm tra xem Username đã tồn tại chưa
            var existingUser = await _userRepository.GetByUsernameAsync(username);
            if (existingUser != null)
            {
                return false; // Tên đăng nhập bị trùng lặp
            }

            // Tìm thông tin Role "Student" để gán mặc định cho người đăng ký mới
            var studentRole = await _roleRepository.GetByNameAsync("Student");
            int defaultRoleId = studentRole?.Id ?? 3; // Fallback về ID 3 (Student) nếu không tìm thấy

            // Mã hóa mật khẩu bằng BCrypt để lưu trữ an toàn chống tấn công Rainbow Table
            string passwordHash = BCryptNet.HashPassword(password);

            var newUser = new User
            {
                Username = username,
                PasswordHash = passwordHash,
                RoleId = defaultRoleId
            };

            // Lưu người dùng mới vào Database thông qua Repository
            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

            return true;
        }
    }
}
