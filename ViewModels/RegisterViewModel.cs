using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class RegisterViewModel
    {
        // =========================================================
        // NAME
        // =========================================================

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(50)]
        [Display(Name = "Name")]
        public string Name { get; set; } = "";


        // =========================================================
        // SURNAME
        // =========================================================

        [Required(ErrorMessage = "Surname is required.")]
        [StringLength(50)]
        [Display(Name = "Surname")]
        public string Surname { get; set; } = "";


        // =========================================================
        // STUDENT NUMBER
        // =========================================================

        [Required(ErrorMessage = "Student Number is required.")]
        [StringLength(20)]
        [Display(Name = "Student Number")]
        public string StudentNumber { get; set; } = "";


        // =========================================================
        // EMAIL
        // =========================================================

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [RegularExpression(
          @"^2[0-9]+@dut4life\.ac\.za$",
          ErrorMessage = "Email must start with your Student Number and end with @dut4life.ac.za.")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";


        // =========================================================
        // PHONE NUMBER
        // =========================================================

        [Required(ErrorMessage = "Phone Number is required.")]
        [RegularExpression(
          @"^(?:0[6-8][0-9]{8}|\+27[6-8][0-9]{8})$",
            ErrorMessage = "Please enter a valid South African phone number.")]
        [Display(Name = "Phone Number")]
        [RegularExpression(@"^(\+27|0)(6[0-9]|7[0-9]|8[0-9])[0-9]{7}$", ErrorMessage = "Please enter a valid South African phone number, e.g. 0821234567 or +27821234567.")]
        public string PhoneNumber { get; set; } = "";


        // =========================================================
        // PASSWORD
        // =========================================================

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MinLength(
            8,
            ErrorMessage = "Password must be at least 8 characters.")]
        [RegularExpression(
          @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[^a-zA-Z0-9]).+$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character.")]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";


        // =========================================================
        // CONFIRM PASSWORD
        // =========================================================

        [Required(ErrorMessage = "Please confirm your password.")]
        [Compare(
            "Password",
            ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = "";
    }
}