using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class Exercise
    {
        [Key]
        public int ExerciseId { get; set; }

        [Required]
        [StringLength(100)]
        public string ExerciseName { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "";

        [Required]
        [StringLength(30)]
        public string Difficulty { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string MuscleGroup { get; set; } = "";

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(2000)]
        public string? Instructions { get; set; }
    }
}