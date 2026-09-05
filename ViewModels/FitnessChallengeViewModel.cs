using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class FitnessChallengeViewModel
    {
        [Required]
        public string FitnessLevel { get; set; } = "";

        [Required]
        public string ExercisePreference { get; set; } = "";

        public List<string> Exercises { get; set; }
            = new List<string>();
    }
}
