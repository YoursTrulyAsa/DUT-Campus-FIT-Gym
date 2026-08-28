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
        [Display(Name = "Email")]
        public string Email { get; set; } = "";


        // =========================================================
        // PHONE NUMBER
        // =========================================================

        [Required(ErrorMessage = "Phone Number is required.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = "";


        // =========================================================
        // PASSWORD
        // =========================================================

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MinLength(
            8,
            ErrorMessage = "Password must be at least 8 characters.")]
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