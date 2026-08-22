using System;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class CheckInViewModel
    {
        // MEMBER
        public int MemberId { get; set; }
        public string FullName { get; set; }
        public string StaffStudentNumber { get; set; }

        // MEMBERSHIP
        public int MembershipId { get; set; }
        public string MembershipType { get; set; }
        public DateTime MembershipEndDate { get; set; }
        public bool MembershipActive { get; set; }

        // CHECK-IN
        public bool IsCheckedIn { get; set; }
        public DateTime? CheckInTime { get; set; }

        // QR
        public string QRCode { get; set; }
    }
}