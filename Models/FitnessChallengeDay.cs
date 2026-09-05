using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class FitnessChallengeDay
    {
        [Key]
        public int FitnessChallengeDayId { get; set; }

        public int FitnessChallengeId { get; set; }

        public int DayNumber { get; set; }

        public string Exercise { get; set; } = "";

        public int Target { get; set; }

        public int Completed { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public FitnessChallenge? FitnessChallenge { get; set; }
    }
}
