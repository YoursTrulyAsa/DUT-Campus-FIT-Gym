using System;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class CheckInResultViewModel
    {
        public string FullName { get; set; }

        public string GymName { get; set; }

        public DateTime CheckInTime { get; set; }

        public bool AccessGranted { get; set; }

        public string Message { get; set; }
    }
}