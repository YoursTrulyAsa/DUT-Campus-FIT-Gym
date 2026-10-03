using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class WorkoutPlan
    {
        [Key]
        public int WorkoutPlanId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public virtual Member? Member { get; set; }

        public int? WorkoutProgrammeId { get; set; }

        [ForeignKey("WorkoutProgrammeId")]
        public virtual WorkoutProgramme? WorkoutProgramme { get; set; }

        [Required]
        [StringLength(100)]
        public string WorkoutName { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string ExerciseName { get; set; } = "";

        public int? ExerciseId { get; set; }

        [ForeignKey("ExerciseId")]
        public virtual Exercise? Exercise { get; set; }

        [Required]
        [StringLength(30)]
        public string Level { get; set; } = "Beginner";

        [Required]
        [StringLength(20)]
        public string WorkoutDay { get; set; } = "";

        [Required]
        [Range(1, 4)]
        public int WeekNumber { get; set; } = 1;

        [Required]
        [Range(1, 100)]
        public int Sets { get; set; }

        [Required]
        [Range(1, 500)]
        public int Repetitions { get; set; }

        [Required]
        [Range(1, 3600)]
        public int RestTime { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }
    }
}