using DataAccessLayer.Models;

namespace DataAccessLayer.Repositories
{
    // Giao diện Repository để truy xuất dữ liệu Role
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(int id);
        Task<Role?> GetByNameAsync(string roleName);
    }
}
