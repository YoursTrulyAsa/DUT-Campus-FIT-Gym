using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class Membership
    {
        [Key]
        public int MembershipId { get; set; }
        public int? MemberId { get; set; }
        public virtual Member? Member { get; set; }

        public string? MembershipType { get; set; }
        public string? PaymentMethod { get; set; }
        public bool FirstTimeMember { get; set; }

        // Make these nullable since they might not be set until payment is complete
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string Status { get; set; } = "WaitingForPayment";
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // Payment tracking fields
        public string? PaymentReference { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentStatus { get; set; }
    }
}