using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class WorkoutCompletion
    {
        [Key]
        public int WorkoutCompletionId { get; set; }

        public int? WorkoutPlanId { get; set; }

        [ForeignKey("WorkoutPlanId")]
        public WorkoutPlan? WorkoutPlan { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        [Required]
        [StringLength(100)]
        public string WorkoutName { get; set; } = "";

        public DateTime CompletedAt { get; set; } = DateTime.Now;

        public int RewardPoints { get; set; } = 7;
    }
}