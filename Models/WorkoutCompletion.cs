using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class WorkoutCompletion
    {
        [Key]
        public int WorkoutCompletionId { get; set; }

        [Required]
        public int WorkoutProgrammeId { get; set; }

        [ForeignKey("WorkoutProgrammeId")]
        public WorkoutProgramme? WorkoutProgramme { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        [Required]
        public int WeekNumber { get; set; }

        [Required]
        [StringLength(20)]
        public string WorkoutDay { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string WorkoutName { get; set; } = "";

        [Required]
        [StringLength(30)]
        public string FitnessLevel { get; set; } = "Beginner";

        public DateTime CompletedAt { get; set; } = DateTime.Now;

        public int RewardPoints { get; set; }
    }
}