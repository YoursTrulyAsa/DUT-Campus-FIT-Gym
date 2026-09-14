using DUT_Campus_FIT_Gym.Models;
using Org.BouncyCastle.Asn1.Cmp;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class AttendanceViewModel
    {
        public int AttendanceId { get; set; }

        public DateTime CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public TimeSpan? Duration { get; set; }

        public int EquipmentCount { get; set; }

        public int EquipmentRewardPoints { get; set; }

        public int CheckInRewardPoints { get; set; }

        public int TotalRewardPoints { get; set; }
    }
}
