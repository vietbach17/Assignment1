using DataAccessLayer.Models;

namespace DataAccessLayer.IRepositories
{
    // Giao diện Repository để truy xuất dữ liệu Role
    public interface IRoleRepository
    {
        Task<IEnumerable<Role>> GetAllAsync();
        Task<Role?> GetByIdAsync(int id);
        Task<Role?> GetByNameAsync(string roleName);
        Task AddAsync(Role role);
        Task UpdateAsync(Role role);
        Task DeleteAsync(Role role);
    }
}
