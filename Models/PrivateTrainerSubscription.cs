using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class PrivateTrainerSubscription
    {
        [Key]
        public int PrivateTrainerSubscriptionId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [Required]
        public int TrainerId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "PendingPayment";

        [StringLength(100)]
        public string? PaymentReference { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        [ForeignKey("TrainerId")]
        public Trainer? Trainer { get; set; }
    }
}