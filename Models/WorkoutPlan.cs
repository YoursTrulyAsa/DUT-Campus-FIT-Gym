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
        public Member? Member { get; set; }

        [StringLength(100)]
        public string WorkoutName { get; set; }

        [StringLength(100)]
        public string ExerciseName { get; set; }

        [StringLength(50)]
        public string WorkoutDay { get; set; }

        public int Sets { get; set; }

        public int Repetitions { get; set; }

        public int RestTime { get; set; }

        [StringLength(500)]
        public string Description { get; set; }
    }
}
