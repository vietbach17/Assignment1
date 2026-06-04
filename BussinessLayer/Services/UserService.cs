using BussinessLayer.DTOs;
using DataAccessLayer.Repositories;

namespace BussinessLayer.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        public UserService(IUserRepository userRepository, IRoleRepository roleRepository)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllUsersAsync();
            return users.Select(MapToDto).ToList();
        }

        public async Task<UserDto?> GetUserByIdAsync(int id)
        {
            var u = await _userRepository.GetByIdAsync(id);
            return u == null ? null : MapToDto(u);
        }

        public async Task<(bool Success, string Message)> UpdateUserAsync(UpdateUserDto dto)
        {
            var user = await _userRepository.GetByIdAsync(dto.Id);
            if (user == null)
                return (false, "Không tìm thấy người dùng.");

            if (user.Id == 1 && dto.RoleId != user.RoleId)
                return (false, "Không thể thay đổi vai trò của tài khoản Admin gốc.");

            var role = await _roleRepository.GetByIdAsync(dto.RoleId);
            if (role == null)
                return (false, "Vai trò không hợp lệ.");

            user.Email = dto.Email.Trim().ToLowerInvariant();
            user.RoleId = dto.RoleId;

            await _userRepository.UpdateAsync(user);
            return (true, $"Đã cập nhật thông tin người dùng \"{user.Username}\" thành công.");
        }

        public async Task<(bool Success, string Message)> DisableUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return (false, "Không tìm thấy người dùng.");

            if (user.Id == 1)
                return (false, "Không thể vô hiệu hóa tài khoản Admin gốc.");

            if (user.IsDeleted)
                return (false, $"Tài khoản \"{user.Username}\" đã bị vô hiệu hóa trước đó.");

            user.IsDeleted = true;
            await _userRepository.UpdateAsync(user);
            return (true, $"Đã vô hiệu hóa tài khoản \"{user.Username}\". Tài khoản này sẽ không thể đăng nhập.");
        }

        public async Task<(bool Success, string Message)> RestoreUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                return (false, "Không tìm thấy người dùng.");

            if (!user.IsDeleted)
                return (false, $"Tài khoản \"{user.Username}\" vẫn đang hoạt động bình thường.");

            user.IsDeleted = false;
            await _userRepository.UpdateAsync(user);
            return (true, $"Đã khôi phục tài khoản \"{user.Username}\" thành công.");
        }

        private static UserDto MapToDto(DataAccessLayer.Models.User u) => new()
        {
            Id        = u.Id,
            Username  = u.Username,
            Email     = u.Email,
            RoleId    = u.RoleId,
            RoleName  = u.Role?.RoleName ?? "N/A",
            IsDeleted = u.IsDeleted
        };
    }
}
