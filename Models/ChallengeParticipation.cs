using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class ChallengeParticipation
    {
        [Key]
        public int ChallengeParticipationId { get; set; }

        [Required]
        public int FitnessChallengeId { get; set; }

        public FitnessChallenge? FitnessChallenge { get; set; }

        [Required]
        public int MemberId { get; set; }

        public Member? Member { get; set; }

        public DateTime AcceptedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Accepted";
    }
}