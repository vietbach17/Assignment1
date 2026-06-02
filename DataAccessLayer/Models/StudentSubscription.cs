using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models
{
    [Table("StudentSubscriptions")]
    public class StudentSubscription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int SubscriptionPlanId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public int RemainingQuestions { get; set; }

        // Thời điểm bắt đầu chu kỳ 24h (khi user đặt câu hỏi đầu tiên trong ngày)
        // Null = chưa đặt câu hỏi nào trong chu kỳ hiện tại
        public DateTime? DailyResetTime { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        [ForeignKey(nameof(SubscriptionPlanId))]
        public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;
    }
}