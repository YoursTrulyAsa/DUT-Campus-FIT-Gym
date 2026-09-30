namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class AIWorkoutExerciseViewModel
    {
        public int WeekNumber { get; set; }
        public string WorkoutDay { get; set; } = "";
        public string ExerciseName { get; set; } = "";
        public int Sets { get; set; }
        public int Repetitions { get; set; }
        public int RestTime { get; set; }
        public string Description { get; set; } = "";
    }
}