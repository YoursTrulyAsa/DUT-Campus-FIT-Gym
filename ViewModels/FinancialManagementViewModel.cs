using DUT_Campus_FIT_Gym.Models;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class FinancialManagementViewModel
    {
        // ============================================================
        // PAID REVENUE
        // ============================================================

        /// <summary>
        /// Total money received from memberships marked as Paid.
        /// </summary>
        public decimal PaidRevenue { get; set; }

        /// <summary>
        /// Number of completed membership payments.
        /// </summary>
        public int TotalPayments { get; set; }

        /// <summary>
        /// Total paid revenue from Student memberships.
        /// </summary>
        public decimal StudentPaidRevenue { get; set; }

        /// <summary>
        /// Total paid revenue from Staff memberships.
        /// </summary>
        public decimal StaffPaidRevenue { get; set; }


        // ============================================================
        // ACTIVE MEMBERSHIPS
        // ============================================================

        /// <summary>
        /// Number of currently active memberships.
        /// </summary>
        public int ActiveMemberships { get; set; }

        /// <summary>
        /// Total value of currently active memberships.
        /// </summary>
        public decimal ActiveMembershipValue { get; set; }

        /// <summary>
        /// Number of active Student memberships.
        /// </summary>
        public int ActiveStudentMemberships { get; set; }

        /// <summary>
        /// Number of active Staff memberships.
        /// </summary>
        public int ActiveStaffMemberships { get; set; }


        // ============================================================
        // COMPLETED PAYMENT RECORDS
        // ============================================================

        /// <summary>
        /// List of memberships with completed payments.
        /// </summary>
        public List<Membership> Payments { get; set; } = new();
    }
}