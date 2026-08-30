using System;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class ReservationViewModel
    {
        public string EquipmentName { get; set; } = "";

        public int ReservationID { get; set; }

        public int MemberID { get; set; }

        public DateTime ReservationDate { get; set; }

        public DateTime EndTime { get; set; }

        public string Status { get; set; } = "Reserved";
    }
}