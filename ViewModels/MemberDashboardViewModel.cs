using DUT_Campus_FIT_Gym.Models;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class MemberDashboardViewModel
    {
        public Member Member { get; set; } = null!;

        public Membership? Membership { get; set; }

        public MembershipApplication? LatestApplication { get; set; }

        public int AttendanceCount { get; set; }

        public int ReservationCount { get; set; }

        public int TotalReservations { get; set; }

        public double TotalGymMinutes { get; set; }

        public double AverageVisitMinutes { get; set; }

        public double WeeklyGymMinutes { get; set; }

        public double EstimatedCalories { get; set; }

        public double WeeklyEstimatedCalories { get; set; }

        public List<string> WeeklyLabels { get; set; } = new();

        public List<int> WeeklyVisitCounts { get; set; } = new();

        public List<WorkoutPlan> Workouts { get; set; } = new();

        public WorkoutProfile? WorkoutProfile { get; set; }

        public List<Attendance> RecentAttendance { get; set; } = new();
    }
}