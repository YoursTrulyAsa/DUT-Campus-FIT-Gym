using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class FitnessChallenge
    {
        [Key]
        public int FitnessChallengeId { get; set; }

        public int MemberId { get; set; }

        public string FitnessLevel { get; set; } = "";

        public string ExerciseType { get; set; } = "";

        public string Title { get; set; } = "";

        public int DurationDays { get; set; }

        public string Status { get; set; } = "Generated";

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? AcceptedDate { get; set; }
        public int PointsAwarded { get; set; } = 0;

        public Member? Member { get; set; }

        public ICollection<FitnessChallengeDay> Days { get; set; }
            = new List<FitnessChallengeDay>();
    }
}
