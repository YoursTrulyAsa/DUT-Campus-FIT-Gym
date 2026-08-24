using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class Equipment
    {
        [Key]
        public int EquipmentID { get; set; }

        [Required]
        public string EquipmentName { get; set; } = "";

        [Required]
        public string Category { get; set; } = "";

        public bool IsAvailable { get; set; } = true;

        [Required]
        public string Location { get; set; } = "";
    }
}