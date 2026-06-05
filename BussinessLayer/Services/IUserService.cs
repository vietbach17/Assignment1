using BussinessLayer.DTOs;

namespace BussinessLayer.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(int id);
        Task<(bool Success, string Message)> UpdateUserAsync(UpdateUserDto dto);
        /// <summary>Vô hiệu hóa tài khoản (soft delete) — không thể đăng nhập</summary>
        Task<(bool Success, string Message)> DisableUserAsync(int id);
        /// <summary>Khôi phục tài khoản đã bị vô hiệu hóa</summary>
        Task<(bool Success, string Message)> RestoreUserAsync(int id);
    }
}
