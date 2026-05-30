using System.ComponentModel.DataAnnotations;

namespace BussinessLayer.DTOs
{
    public class SubscriptionPlanDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên gói không được để trống")]
        [StringLength(100, ErrorMessage = "Tên gói không quá 100 ký tự")]
        public string Name { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Mô tả không quá 500 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Giá gói không được để trống")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá gói phải lớn hơn hoặc bằng 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Giới hạn câu hỏi không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "Giới hạn câu hỏi phải lớn hơn 0")]
        public int QuestionLimit { get; set; }
    }
}