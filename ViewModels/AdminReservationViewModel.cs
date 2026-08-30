namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class AdminReservationViewModel
    {
        public int ReservationID { get; set; }

        public string MemberName { get; set; } = "";

        public string StudentNumber { get; set; } = "";

        public string EquipmentName { get; set; } = "";

        public DateTime ReservationDate { get; set; }

        public string Status { get; set; } = "";
    }
}
