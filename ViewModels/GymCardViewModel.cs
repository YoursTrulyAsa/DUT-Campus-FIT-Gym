using System;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class GymCardViewModel
    {
        public int MemberId { get; set; }
        public string FullName { get; set; }
        public string StaffStudentNumber { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }

        public int MembershipId { get; set; }
        public string MembershipType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; }

        public int DaysRemaining { get; set; }

        public string Barcode { get; set; }
    }
}