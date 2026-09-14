using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.ViewModels
{
    public class MembershipPage
    {
        public string? Name { get; set; }

        public string? Surname { get; set; }

        public string? Email { get; set; }

        public string? StudentNo { get; set; }

        [Required(ErrorMessage = "Please select a membership period.")]
        public string? MembershipPeriod { get; set; }


        [Required(ErrorMessage = "Please select a payment method.")]
        public string? PaymentMethod { get; set; }


        [Required(ErrorMessage = "Please upload your student/staff card.")]
        public IFormFile? VerificationDocument { get; set; }
    }
}