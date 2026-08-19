using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string StaffStudentNumber { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }

        [Required]
        public string Role { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [MinLength(8)]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
        
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
