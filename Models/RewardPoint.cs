using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class RewardPoint
    {
        [Key]
        public int RewardPointId { get; set; }

        [Required]
        public int MemberId { get; set; }

        public Member? Member { get; set; }

        [Required]
        public int Points { get; set; }

        [Required]
        [StringLength(100)]
        public string Reason { get; set; } = "";

        public DateTime EarnedAt { get; set; } = DateTime.Now;
    }
}