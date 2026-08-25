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

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Role == "Student")
            {
                string studentPattern = @"^[0-9]{8}@dut4life\.ac\.za$";
                if (!Regex.IsMatch(Email, studentPattern, RegexOptions.IgnoreCase))
                {
                    yield return new ValidationResult(
                        "Student email must start with an 8-digit student number and end with @dut4life.ac.za",
                        new[] { nameof(Email) });
                }
            }
            else if (Role == "Staff")
            {
                string staffPattern = @"^[A-Za-z]+@dut\.ac\.za$";
                if (!Regex.IsMatch(Email, staffPattern, RegexOptions.IgnoreCase))
                {
                    yield return new ValidationResult(
                        "Staff email must start with a name and end with @dut.ac.za",
                        new[] { nameof(Email) });
                }
            }
        }

    }
}