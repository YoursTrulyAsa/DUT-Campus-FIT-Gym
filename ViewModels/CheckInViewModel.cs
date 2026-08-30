using System;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class CheckInViewModel
    {
        public string FullName { get; set; } = string.Empty;

        public bool MembershipActive { get; set; }

        public bool IsCheckedIn { get; set; }

        public DateTime? CheckInTime { get; set; }
    }
}