using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        // Link payment to a specific member
        [ForeignKey("Member")]
        public int MemberId { get; set; }
        public Member Member { get; set; }

        // Amount paid
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        // Payment method (Cash, Card, EFT, etc.)
        [Required]
        [MaxLength(50)]
        public string Method { get; set; }

        // Date of payment
        [Required]
        public DateTime PaymentDate { get; set; }

        // Status (Pending, Approved, Declined)
        [Required]
        [MaxLength(20)]
        public string Status { get; set; }

        // Optional reference number (receipt, transaction ID)
        [MaxLength(100)]
        public string ReferenceNumber { get; set; }
    }
}
