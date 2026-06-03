namespace BussinessLayer.DTOs
{
    // Data Transfer Object (DTO) dùng để vận chuyển dữ liệu người dùng an toàn giữa các lớp
    // Bằng cách này, chúng ta che giấu thực thể cơ sở dữ liệu User thực tế (không rò rỉ PasswordHash)
    public class UserDto
    {
        public int Id { get; set; }
        
        public string Username { get; set; } = string.Empty;
        
        public string RoleName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}
