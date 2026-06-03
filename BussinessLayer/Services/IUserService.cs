using BussinessLayer.DTOs;

namespace BussinessLayer.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(int id);
    }
}
