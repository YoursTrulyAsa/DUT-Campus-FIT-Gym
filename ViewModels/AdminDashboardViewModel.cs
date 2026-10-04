using DUT_Campus_FIT_Gym.Models;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalMembers { get; set; }

        public int PendingApplications { get; set; }

        public int ActiveMemberships { get; set; }

        public int AvailableEquipment { get; set; }

        public int UnavailableEquipment { get; set; }

        public int ActiveReservations { get; set; }

        public List<MembershipApplication> RecentApplications { get; set; }
            = new List<MembershipApplication>();

        public int StudentMembers { get; set; }

        public int StaffMembers { get; set; }

        public int ExpiredMemberships { get; set; }

        public int WaitingForPayment { get; set; }

        public int ApprovedApplications { get; set; }

        public int RejectedApplications { get; set; }

        public int NewApplicationsThisMonth { get; set; }

        public int TotalCheckIns { get; set; }

        public int TodayCheckIns { get; set; }

        public int CurrentMonthCheckIns { get; set; }

        public decimal TotalPaidRevenue { get; set; }

        public decimal CurrentMonthRevenue { get; set; }

        public decimal StudentRevenue { get; set; }

        public decimal StaffRevenue { get; set; }

        public int TotalWorkoutProgrammes { get; set; }

        public int ActiveWorkoutProgrammes { get; set; }

        public int CompletedWorkoutProgrammes { get; set; }

        public int FavouriteWorkoutProgrammes { get; set; }

        public int SharedWorkoutResults { get; set; }

        public int SavedWorkoutResults { get; set; }

        public Dictionary<string, int> MembershipTypeCounts { get; set; }
            = new Dictionary<string, int>();

        public Dictionary<string, int> ApplicationStatusCounts { get; set; }
            = new Dictionary<string, int>();

        public Dictionary<string, int> DailyCheckIns { get; set; }
            = new Dictionary<string, int>();

        public Dictionary<string, int> MembershipStatusCounts { get; set; }
            = new Dictionary<string, int>();
    }
}