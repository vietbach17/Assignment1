namespace BussinessLayer.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string Email { get; set; } = string.Empty;
        /// <summary>Soft delete — tài khoản bị vô hiệu hóa, không thể đăng nhập</summary>
        public bool IsDeleted { get; set; }
    }

    public class UpdateUserDto
    {
        public int Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Email không được để trống")]
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [System.ComponentModel.DataAnnotations.MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Vui lòng chọn vai trò")]
        public int RoleId { get; set; }
    }
}
