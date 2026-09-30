using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class MonthlyLeaderboard
    {
        [Key]
        public int MonthlyLeaderboardId { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public int MemberId { get; set; }

        public Member? Member { get; set; }

        [Required]
        public int Rank { get; set; }

        [Required]
        public int Points { get; set; }

        [Required]
        [StringLength(30)]
        public string Standing { get; set; } = "Normal";

        [Required]
        [StringLength(30)]
        public string GiftStatus { get; set; } = "NotApplicable";

        public bool NotificationSent { get; set; }

        public DateTime FinalizedAt { get; set; } = DateTime.Now;
    }
}
