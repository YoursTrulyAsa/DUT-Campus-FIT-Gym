using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class SavedWorkoutResult
    {
        [Key]
        public int SavedWorkoutResultId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        [Required]
        public int WorkoutResultId { get; set; }

        [ForeignKey("WorkoutResultId")]
        public WorkoutResult? WorkoutResult { get; set; }

        public DateTime SavedAt { get; set; } = DateTime.Now;
    }
}