using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class WorkoutCompletion
    {
        [Key]
        public int WorkoutCompletionId { get; set; }

    [Required]
        public int MemberId { get; set; }

        public Member? Member { get; set; }

        [Required]
        [StringLength(100)]
        public string WorkoutName { get; set; } = "";

        public DateTime CompletedAt { get; set; } = DateTime.Now;

        public int RewardPoints { get; set; } = 7;
    }
}
