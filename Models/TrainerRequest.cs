using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class TrainerRequest
    {
        [Key]
        public int TrainerRequestId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int TrainerId { get; set; }

        [Required]
        [StringLength(500)]
        public string RequestMessage { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Pending";

        public DateTime RequestDate { get; set; } = DateTime.Now;

        public DateTime? ResponseDate { get; set; }

        public DateTime? CompletionDate { get; set; }

        [ForeignKey("StudentId")]
        public Member? Student { get; set; }

        [ForeignKey("TrainerId")]
        public Trainer? Trainer { get; set; }
    }
}