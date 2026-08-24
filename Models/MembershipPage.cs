namespace DUT_Campus_FIT_Gym.Models
{
    public class MembershipPage
    {
        public enum PAY
        {
            Monthly,
            Half_Yearly,
            Quarterly,
            Annually
        }

        public string Name { get; set; } = string.Empty;

        public string Surname { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string StudentNo { get; set; } = string.Empty;

        public string Payment_Method { get; set; } = string.Empty;

        public PAY payments_plan { get; set; }

        public bool First_Time_Member { get; set; }

        // Server-controlled values
        // These are NOT trusted from the browser.
        public bool CanClaimFirstTimeDiscount { get; set; }

        public bool IsRenewal { get; set; }
    }
}