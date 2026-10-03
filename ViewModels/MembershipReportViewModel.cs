using DUT_Campus_FIT_Gym.Models;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class MembershipReportViewModel
    {
        public int TotalMemberships { get; set; }

        public int ActiveMemberships { get; set; }

        public int ExpiredMemberships { get; set; }

        public int WaitingForPayment { get; set; }

        public List<Membership> Memberships { get; set; } = new();
    }
}
