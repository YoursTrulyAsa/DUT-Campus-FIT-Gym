using System;
using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }

        public int MemberId { get; set; }

        public DateTime CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        // Optional navigation property
        public Member? Member { get; set; }
    }
}