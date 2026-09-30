namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class FitnessLeaderboardViewModel
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public string MonthName { get; set; } = "";

        public int TotalStudents { get; set; }

        public int TopScore { get; set; }

        public double AverageScore { get; set; }

        public List<FitnessLeaderboardEntryViewModel> Entries { get; set; }
            = new List<FitnessLeaderboardEntryViewModel>();
    }

    public class FitnessLeaderboardEntryViewModel
    {
        public int Rank { get; set; }

        public int MemberId { get; set; }

        public string Name { get; set; } = "";

        public string Surname { get; set; } = "";

        public string FullName =>
            $"{Name} {Surname}".Trim();

        public int Points { get; set; }

        public bool IsTopThree { get; set; }

        public bool IsBottom { get; set; }
    }
}