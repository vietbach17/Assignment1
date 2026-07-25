namespace BussinessLayer.DTOs
{
    // DTO đại diện cho Vai trò khi truyền lên tầng Presentation
    // (tránh lộ entity DataAccessLayer.Models.Role ra ngoài tầng nghiệp vụ)
    public class RoleDto
    {
        public int Id { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}
