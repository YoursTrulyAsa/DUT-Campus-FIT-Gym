using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class MembershipPage
    {
        // =========================================================
        // MEMBER INFORMATION
        // =========================================================

        public string? Name { get; set; }

        public string? Surname { get; set; }

        public string? Email { get; set; }

        public string? StudentNo { get; set; }


        // =========================================================
        // MEMBERSHIP
        // =========================================================

        [Required(ErrorMessage = "Please select a membership period.")]
        public string? MembershipPeriod { get; set; }


        // =========================================================
        // PAYMENT
        // =========================================================

        [Required(ErrorMessage = "Please select a payment method.")]
        public string? PaymentMethod { get; set; }


        // =========================================================
        // VERIFICATION DOCUMENT
        // =========================================================

        [Required(ErrorMessage = "Please upload your student/staff card.")]
        public IFormFile? VerificationDocument { get; set; }
    }
}