
namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class FitnessInsightsViewModel
    {
        public string FitnessGoal { get; set; } = "Not set";

        public int Age { get; set; }

        public double Weight { get; set; }

        public double Height { get; set; }

        public string MembershipStatus { get; set; } = "No Membership";

        public string MembershipType { get; set; } = "";

        public int MembershipDaysRemaining { get; set; }

        public int TotalGymVisits { get; set; }

        public int CompletedVisits { get; set; }

        public double TotalGymHours { get; set; }

        public double AverageVisitMinutes { get; set; }

        public int WeeklyVisits { get; set; }

        public double WeeklyGymHours { get; set; }

        public int SavedWorkoutExercises { get; set; }

        public int WorkoutDays { get; set; }

        public int TotalSets { get; set; }

        public int TotalRepetitions { get; set; }

        public int SavedMealPlans { get; set; }

        public int TotalRewardPoints { get; set; }
    }
}