using DUT_Campus_FIT_Gym.Models;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class AdminDashboardViewModel
    {
        // ==========================================
        // DASHBOARD STATISTICS
        // ==========================================

        public int TotalMembers { get; set; }

        public int PendingApplications { get; set; }

        public int ActiveMemberships { get; set; }

        public int AvailableEquipment { get; set; }

        public int UnavailableEquipment { get; set; }

        public int ActiveReservations { get; set; }


        // ==========================================
        // RECENT APPLICATIONS
        // ==========================================

        public List<MembershipApplication> RecentApplications { get; set; }
            = new List<MembershipApplication>();
    }
}