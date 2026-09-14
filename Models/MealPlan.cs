using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class MealPlan
    {
        [Key]
        public int MealPlanId { get; set; }

        [Required]
        public int MemberId { get; set; }

        [Required]
        [StringLength(50)]
        public string FitnessGoal { get; set; } = "";

        [StringLength(100)]
        public string DietaryPreference { get; set; } = "";

        [StringLength(500)]
        public string FoodPreferences { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public Member Member { get; set; } = null!;

        public ICollection<MealPlanItem> MealPlanItems { get; set; }
            = new List<MealPlanItem>();
    }
}