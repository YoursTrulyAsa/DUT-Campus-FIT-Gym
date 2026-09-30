namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class MonthlyLeaderboardViewModel
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public string MonthName { get; set; } = "";

        public bool IsFinalized { get; set; }

        public DateTime? FinalizedAt { get; set; }

        public List<MonthlyLeaderboardEntryViewModel> Entries { get; set; }
            = new List<MonthlyLeaderboardEntryViewModel>();
    }

    public class MonthlyLeaderboardEntryViewModel
    {
        public int MonthlyLeaderboardId { get; set; }

        public int Rank { get; set; }

        public int MemberId { get; set; }

        public string Name { get; set; } = "";

        public string Surname { get; set; } = "";

        public string FullName =>
            $"{Name} {Surname}".Trim();

        public string Email { get; set; } = "";

        public int Points { get; set; }

        public string Standing { get; set; } = "Normal";

        public string GiftStatus { get; set; } = "NotApplicable";

        public bool NotificationSent { get; set; }
    }
}