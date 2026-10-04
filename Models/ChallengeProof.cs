using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class ChallengeProof
    {
        [Key]
        public int ChallengeProofId { get; set; }

        [Required]
        public int FitnessChallengeId { get; set; }

        [ForeignKey("FitnessChallengeId")]
        public FitnessChallenge? FitnessChallenge { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        [Required]
        [StringLength(500)]
        public string VideoPath { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "PendingReview";

        [StringLength(500)]
        public string? TrainerComment { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        public DateTime? ReviewedAt { get; set; }

        public int? ReviewedByTrainerId { get; set; }

        [ForeignKey("ReviewedByTrainerId")]
        public Trainer? ReviewedByTrainer { get; set; }
    }
}