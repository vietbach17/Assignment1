using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByUsernameAsync(string username);
        Task<IEnumerable<User>> GetUsersByRoleIdAsync(int roleId);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task AddAsync(User user);
        Task UpdateAsync(User user);
        Task SaveChangesAsync();
    }
}
