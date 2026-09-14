namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class AIMealSuggestionViewModel
    {
        public string MealType { get; set; } = "";
        public string MealName { get; set; } = "";
        public string Description { get; set; } = "";
        public List<string> Ingredients { get; set; } = new();
        public string Preparation { get; set; } = "";
    }
}