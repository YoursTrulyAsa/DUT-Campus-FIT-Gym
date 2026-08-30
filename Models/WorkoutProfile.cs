using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class WorkoutProfile
    {
        [Key]
        public int WorkoutProfileId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        [Required]
        [Range(13, 100)]
        public int Age { get; set; }

        [Required]
        [Range(1, 500)]
        public double Weight { get; set; }

        [Required]
        [Range(1, 300)]
        public double Height { get; set; }

        [Required]
        [StringLength(100)]
        public string Goal { get; set; } = "";
    }
}