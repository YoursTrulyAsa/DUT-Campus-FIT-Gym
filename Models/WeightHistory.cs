using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class WeightHistory
    {
        [Key]
        public int WeightHistoryId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        [Required]
        [Range(1, 500)]
        public double Weight { get; set; }

        [Required]
        public DateTime RecordedAt { get; set; } = DateTime.Now;
    }
}