using System;

namespace BussinessLayer.DTOs
{
    public class StudentSubscriptionDTO
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SubscriptionPlanId { get; set; }

        // Ta kéo luôn tên gói và mô tả gói vào đây để View hiển thị trực tiếp cực kỳ tiện lợi
        public string PlanName { get; set; } = null!;
        public string? PlanDescription { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int RemainingQuestions { get; set; }

        // Thời điểm bắt đầu chu kỳ 24h reset
        public DateTime? DailyResetTime { get; set; }

        // Tổng giới hạn câu hỏi/ngày của gói hiện tại (để view hiển thị)
        public int QuestionLimit { get; set; }
    }
}