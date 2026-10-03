using DUT_Campus_FIT_Gym.Models;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class AttendanceReportViewModel
    {
        public int TotalVisits { get; set; }

        public int CurrentMonthVisits { get; set; }

        public int TodayVisits { get; set; }

        public List<Attendance> Attendances { get; set; } = new();
    }
}
