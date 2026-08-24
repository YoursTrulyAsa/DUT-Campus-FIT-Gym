using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        [Required]
        public int MembershipId { get; set; }

        [ForeignKey("MembershipId")]
        public Membership? Membership { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "";

        [Required]
        [StringLength(30)]
        public string PaymentStatus { get; set; } = "Paid";

        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(50)]
        public string ReceiptNumber { get; set; } = "";
    }
}