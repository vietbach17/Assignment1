using DataAccessLayer.Models;

namespace BussinessLayer.Services
{
    public interface IRoleService
    {
        Task<IEnumerable<Role>> GetAllRolesAsync();
        Task<Role?> GetRoleByIdAsync(int id);
        Task<bool> CreateRoleAsync(string roleName);
        Task<bool> UpdateRoleAsync(int id, string newRoleName);
        Task<bool> DeleteRoleAsync(int id);
    }
}
