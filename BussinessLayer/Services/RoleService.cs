using DataAccessLayer.Models;
using DataAccessLayer.Repositories;

namespace BussinessLayer.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;
        // Danh sách các Role cốt lõi không được phép sửa hay xoá
        private readonly string[] _protectedRoles = { "Admin", "Lecturer", "Student" };

        public RoleService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<IEnumerable<Role>> GetAllRolesAsync()
        {
            return await _roleRepository.GetAllAsync();
        }

        public async Task<Role?> GetRoleByIdAsync(int id)
        {
            return await _roleRepository.GetByIdAsync(id);
        }

        public async Task<bool> CreateRoleAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName)) return false;

            var existingRole = await _roleRepository.GetByNameAsync(roleName);
            if (existingRole != null) return false;

            var newRole = new Role { RoleName = roleName.Trim() };
            await _roleRepository.AddAsync(newRole);
            return true;
        }

        public async Task<bool> UpdateRoleAsync(int id, string newRoleName)
        {
            if (string.IsNullOrWhiteSpace(newRoleName)) return false;

            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) return false;

            if (_protectedRoles.Contains(role.RoleName, StringComparer.OrdinalIgnoreCase))
                return false;

            var existingName = await _roleRepository.GetByNameAsync(newRoleName);
            if (existingName != null && existingName.Id != id) return false;

            role.RoleName = newRoleName.Trim();
            await _roleRepository.UpdateAsync(role);
            return true;
        }

        public async Task<bool> DeleteRoleAsync(int id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role == null) return false;

            if (_protectedRoles.Contains(role.RoleName, StringComparer.OrdinalIgnoreCase))
                return false;

            try
            {
                await _roleRepository.DeleteAsync(role);
                return true;
            }
            catch
            {
                // Thường do lỗi khoá ngoại (Foreign Key) nếu Role đang được User sử dụng
                return false;
            }
        }
    }
}
