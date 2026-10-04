using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class WorkoutResult
    {
        [Key]
        public int WorkoutResultId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        [Required]
        public int WorkoutProgrammeId { get; set; }

        [ForeignKey("WorkoutProgrammeId")]
        public WorkoutProgramme? WorkoutProgramme { get; set; }

        [Required]
        [StringLength(500)]
        public string BeforePhoto { get; set; } = "";

        [Required]
        [StringLength(500)]
        public string AfterPhoto { get; set; } = "";

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = "";

        public DateTime SharedAt { get; set; } = DateTime.Now;
    }
}