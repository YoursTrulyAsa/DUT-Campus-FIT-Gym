using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class MembershipApplication
    {
        [Key]
        public int MembershipApplicationId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        [Required]
        [StringLength(20)]
        public string MembershipType { get; set; } = "";

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        [Required]
        public DateTime ApplicationDate { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        [Required]
        [StringLength(255)]
        public string VerificationDocument { get; set; } = "";

        public DateTime? ReviewedDate { get; set; }

        public string? AdminComment { get; set; }

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "PayFast";
    }
}