using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DUT_Campus_FIT_Gym.Models
{
    public class EquipmentPenalty
    {
        [Key]
        public int EquipmentPenaltyId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        [Required]
        public int ReservationId { get; set; }

        [ForeignKey("ReservationId")]
        public Reservation? Reservation { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; } = 50.00m;

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Outstanding";

        [Required]
        public DateTime PenaltyDate { get; set; } = DateTime.Now;
    }
}