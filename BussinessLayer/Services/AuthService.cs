using DataAccessLayer.Models;
using DataAccessLayer.Repositories;
using BussinessLayer.DTOs;
using BCryptNet = BCrypt.Net.BCrypt;
using BussinessLayer.Interfaces;

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

        public async Task<UserDto?> LoginAsync(string username, string password)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null)
                return null;

            // Tài khoản bị vô hiệu hóa (soft delete) — không thể đăng nhập
            if (user.IsDeleted)
                return new UserDto { Id = -1, Username = "__DISABLED__" };

            bool isPasswordValid = BCryptNet.Verify(password, user.PasswordHash);
            if (!isPasswordValid)
                return null;

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                RoleName = user.Role.RoleName
            };
        }

        public async Task<bool> RegisterAsync(string username, string password, string email, int roleId)
        {
            // Kiểm tra xem Username đã tồn tại chưa
            var existingUser = await _userRepository.GetByUsernameAsync(username);
            if (existingUser != null)
            {
                return false; // Tên đăng nhập bị trùng lặp
            }

            // Mã hóa mật khẩu bằng BCrypt để lưu trữ an toàn chống tấn công Rainbow Table
            string passwordHash = BCryptNet.HashPassword(password);

            var newUser = new User
            {
                Username = username,
                PasswordHash = passwordHash,
                Email = email.Trim().ToLowerInvariant(),
                RoleId = roleId
            };

            // Lưu người dùng mới vào Database thông qua Repository
            await _userRepository.AddAsync(newUser);
            await _userRepository.SaveChangesAsync();

            return true;
        }
    }
}
