using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.RegularExpressions;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [Display(Name = "Name")]
        public string Name { get; set; } = "";

        [Required]
        [Display(Name = "Surname")]
        public string Surname { get; set; } = "";

        [Display(Name = "Student Number")]
        public string StudentNumber { get; set; } = "";

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required]
        [Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = "";
        [Required]
        public string Role { get; set; } = "";

        [Required]
        [DataType(DataType.Password)]
        [MinLength(8)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [Required]
        [Compare("Password")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }
}