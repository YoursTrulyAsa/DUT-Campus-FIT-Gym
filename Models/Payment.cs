using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        // ==========================================
        // MEMBER
        // ==========================================

        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }


        // ==========================================
        // MEMBERSHIP
        // ==========================================

        public int MembershipId { get; set; }

        [ForeignKey("MembershipId")]
        public Membership? Membership { get; set; }


        // ==========================================
        // PAYMENT DETAILS
        // ==========================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string? PaymentMethod { get; set; }

        public string? PaymentStatus { get; set; }

        public DateTime PaymentDate { get; set; }

        public string? ReceiptNumber { get; set; }
    }
}