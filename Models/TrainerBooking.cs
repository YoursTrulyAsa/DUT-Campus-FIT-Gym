using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class TrainerBooking
    {
        [Key]
        public int TrainerBookingId { get; set; }

        [Required]
        public int TrainerRequestId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int TrainerId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "PendingPayment";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? CompletedDate { get; set; }

        [ForeignKey("TrainerRequestId")]
        public TrainerRequest? TrainerRequest { get; set; }

        [ForeignKey("StudentId")]
        public Member? Student { get; set; }

        [ForeignKey("TrainerId")]
        public Trainer? Trainer { get; set; }
    }
}