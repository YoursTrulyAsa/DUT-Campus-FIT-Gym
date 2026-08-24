using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class Reservation
    {
        [Key]
        public int ReservationID { get; set; }

        [Required]
        public int MemberID { get; set; }

        [Required]
        public int EquipmentID { get; set; }

        [Required]
        public DateTime ReservationDate { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        public string Status { get; set; } = "";
    }
}
