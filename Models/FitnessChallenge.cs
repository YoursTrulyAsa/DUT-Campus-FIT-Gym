using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class FitnessChallenge
    {
        [Key]
        public int FitnessChallengeId { get; set; }

        [Required]
        public int MemberId { get; set; }

        public Member? Member { get; set; }

        [Required]
        [StringLength(30)]
        public string Level { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = "";

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = "";

        [Required]
        [StringLength(500)]
        public string Target { get; set; } = "";

        public int RewardPoints { get; set; } = 5;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Generated";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ChallengeParticipation? Participation { get; set; }
    }
}