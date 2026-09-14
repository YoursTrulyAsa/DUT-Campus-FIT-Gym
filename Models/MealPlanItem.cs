using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class MealPlanItem
    {
        [Key]
        public int MealPlanItemId { get; set; }

        [Required]
        public int MealPlanId { get; set; }

        [Required]
        [StringLength(30)]
        public string MealType { get; set; } = "";

        [Required]
        [StringLength(150)]
        public string MealName { get; set; } = "";

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = "";

        [Required]
        public string Ingredients { get; set; } = "";

        [Required]
        [StringLength(1000)]
        public string Preparation { get; set; } = "";

        public MealPlan MealPlan { get; set; } = null!;
    }
}
