using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class WorkoutProgramme
    {
        [Key]
        public int WorkoutProgrammeId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public virtual Member? Member { get; set; }

        [Required]
        [StringLength(100)]
        public string ProgrammeName { get; set; } = "";

        [Required]
        [StringLength(30)]
        public string FitnessLevel { get; set; } = "Beginner";

        [Required]
        [StringLength(100)]
        public string Goal { get; set; } = "General fitness";

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}