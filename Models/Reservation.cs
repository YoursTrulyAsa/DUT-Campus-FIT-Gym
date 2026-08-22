namespace DUT_Campus_FIT_Gym.Models
{
    public class Reservation
    {
        public int ReservationID { get; set; }

        public int MemberID { get; set; }

        public int EquipmentID { get; set; }

        public DateTime ReservationDate { get; set; }

        public string Status { get; set; } = "";
    }
}
