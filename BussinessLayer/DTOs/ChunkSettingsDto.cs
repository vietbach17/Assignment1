using System.ComponentModel.DataAnnotations;

namespace BussinessLayer.DTOs
{
    public class ChunkSettingsDto
    {
        [Required(ErrorMessage = "Số từ tối đa là bắt buộc")]
        [Range(100, 1000, ErrorMessage = "Số từ tối đa phải từ 100 đến 1000")]
        public int MaxWords { get; set; } = 300;

        [Required(ErrorMessage = "Số từ gối đầu là bắt buộc")]
        [Range(10, 200, ErrorMessage = "Số từ gối đầu phải từ 10 đến 200")]
        public int OverlapWords { get; set; } = 50;
    }
}
