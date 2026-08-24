using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class BankDetails
    {
        [Key]
        public int BankingDetailsID { get; set; }
        [Required]
        [DisplayName("Account Holder Name")]
        public string AccountHolderName { get; set; }
        [Required]
        [DisplayName("Bank Name")]
        public string Bank { get; set; }
        [Required]
        [DisplayName("Card Number")]
        [MinLength(16), MaxLength(16)]
        public string CardNumber { get; set; }
        [Required]
        [DisplayName("Expiry Date")]
        public string ExpiryDate { get; set; }
        [Required]
        [DisplayName("CVV")]
        [MinLength(3), MaxLength(3)]
        public string cvv { get; set; }
        [Required]
        [DisplayName("Type of account")]
        public string AccountType { get; set; }
    }
}
