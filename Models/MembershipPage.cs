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

        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }

        public string StudentNo { get; set; }

        public string Payment_Method { get; set; }

        public PAY payments_plan { get; set; }

        public bool First_Time_Member { get; set; }
    }
}