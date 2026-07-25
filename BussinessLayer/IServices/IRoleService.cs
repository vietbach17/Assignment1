using BussinessLayer.DTOs;

namespace BussinessLayer.IServices
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();
        Task<RoleDto?> GetRoleByIdAsync(int id);
        Task<bool> CreateRoleAsync(string roleName);
        Task<bool> UpdateRoleAsync(int id, string newRoleName);
        Task<bool> DeleteRoleAsync(int id);
    }
}
