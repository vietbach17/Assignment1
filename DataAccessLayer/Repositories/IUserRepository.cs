using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories
{
    // Giao diện (Interface) Repository để tương tác dữ liệu với thực thể User
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<IEnumerable<User>> GetUsersByRoleIdAsync(int roleId);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task AddAsync(User user);
        Task SaveChangesAsync();
    }
}
