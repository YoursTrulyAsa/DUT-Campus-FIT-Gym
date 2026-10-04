using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        public int? MembershipId { get; set; }

        [ForeignKey("MembershipId")]
        public Membership? Membership { get; set; }

        public int? EquipmentPenaltyId { get; set; }

        [ForeignKey("EquipmentPenaltyId")]
        public EquipmentPenalty? EquipmentPenalty { get; set; }

        public int? PrivateTrainerSubscriptionId { get; set; }

        [ForeignKey("PrivateTrainerSubscriptionId")]
        public PrivateTrainerSubscription? PrivateTrainerSubscription { get; set; }

        public int? TrainerBookingId { get; set; }

        [ForeignKey("TrainerBookingId")]
        public TrainerBooking? TrainerBooking { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string? PaymentMethod { get; set; }

        public string? PaymentStatus { get; set; }

        public DateTime PaymentDate { get; set; }

        public string? ReceiptNumber { get; set; }
    }
}