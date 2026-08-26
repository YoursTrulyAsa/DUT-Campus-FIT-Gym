using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class Member
    {
        [Key]
        public int MemberId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = "";

        [Required]
        [StringLength(50)]
        public string Surname { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string StudentNumber { get; set; } = "";

        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Student";

        public string PasswordHash { get; set; } = "";

        public ICollection<WorkoutPlan> WorkoutPlans { get; set; }
            = new List<WorkoutPlan>();

        public ICollection<WorkoutProfile> WorkoutProfiles { get; set; }
            = new List<WorkoutProfile>();

        public ICollection<Membership> Memberships { get; set; }
            = new List<Membership>();

        public ICollection<MembershipApplication> MembershipApplications { get; set; }
            = new List<MembershipApplication>();

        public ICollection<Payment> Payments { get; set; }
            = new List<Payment>();
    }
}