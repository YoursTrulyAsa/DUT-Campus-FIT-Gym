using System.ComponentModel.DataAnnotations;

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

        // Student who requested assistance
        public Member Student { get; set; }

        // Trainer who receives the request
        public Member Trainer { get; set; }
    }
}