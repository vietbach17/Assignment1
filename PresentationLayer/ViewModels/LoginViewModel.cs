using System.ComponentModel.DataAnnotations;

namespace PresentationLayer.ViewModels
{
    // Model nhận dữ liệu đăng nhập từ giao diện và thực hiện kiểm tra tính hợp lệ cơ bản
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }
}
