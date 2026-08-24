using System.ComponentModel.DataAnnotations;

namespace DUT_Campus_FIT_Gym.Models
{
    public class Member
    {
        [Key]
        public int MemberId { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
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

        public string PasswordHash { get; set; }


        // =====================================================
        // WORKOUT RELATIONSHIPS
        // =====================================================

        public ICollection<WorkoutPlan> WorkoutPlans { get; set; }
            = new List<WorkoutPlan>();

        public ICollection<WorkoutProfile> WorkoutProfiles { get; set; }
            = new List<WorkoutProfile>();
    }
}
