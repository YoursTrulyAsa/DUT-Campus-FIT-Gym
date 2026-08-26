using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class Membership
    {
        [Key]
        public int MembershipId { get; set; }

        // Member relationship
        public int? MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        // Membership details
        [Required]
        [StringLength(20)]
        public string MembershipType { get; set; } = "";

        public bool FirstTimeMember { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // Membership dates
        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        // Membership status
        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Pending";

        // Payment details
        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        [StringLength(100)]
        public string? PaymentReference { get; set; }

        public DateTime? PaymentDate { get; set; }

        [StringLength(30)]
        public string? PaymentStatus { get; set; }
    }
}