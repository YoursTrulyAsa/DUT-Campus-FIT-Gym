using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class Equipment
    {
        [Key]
        public int EquipmentID { get; set; }

        [Required]
        [StringLength(100)]
        public string EquipmentName { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "";

        public bool IsAvailable { get; set; } = true;

        [Required]
        [StringLength(100)]
        public string Location { get; set; } = "";
    }
}