using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccessLayer.Models
{
    [Table("PaymentTransactions")]
    public class PaymentTransaction
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int SubscriptionPlanId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public DateTime TransactionDate { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = null!; // "Success" hoặc "Failed"

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
}