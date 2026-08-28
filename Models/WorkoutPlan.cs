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

        [Required]
        [StringLength(100)]
        public string WorkoutName { get; set; } = "";

        [Required]
        [StringLength(150)]
        public string ExerciseName { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string WorkoutDay { get; set; } = "";

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
