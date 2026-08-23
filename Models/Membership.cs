using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class Membership
    {
        [Key]
        public int MembershipId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [Required]
        [StringLength(50)]
        public string MembershipType { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }

        // Payment method selected during membership creation
        [NotMapped]
        [Required]
        public string PaymentMethod { get; set; }

        // Whether this is the member's first membership
        [NotMapped]
        public bool FirstTimeMember { get; set; }

        public Member Member { get; set; }
    }
}