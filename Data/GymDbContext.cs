using DUT_Campus_FIT_Gym.Models;
using Microsoft.EntityFrameworkCore;

namespace DUT_Campus_FIT_Gym.Data
{
    public class GymDbContext : DbContext
    {
        public GymDbContext(DbContextOptions<GymDbContext> options)
            : base(options)
        {
        }

        public DbSet<Member> Members { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<MembershipApplication> MembershipApplications { get; set; }

        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        public DbSet<Announcement> Announcements { get; set; }

        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<TrainerRequest> TrainerRequests { get; set; }

        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
        public DbSet<WorkoutProfile> WorkoutProfiles { get; set; }

        public DbSet<BankDetails> BankingDetails { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public DbSet<MealPlan> MealPlans { get; set; }
        public DbSet<MealPlanItem> MealPlanItems { get; set; }

        public DbSet<FitnessChallenge> FitnessChallenges { get; set; }
        public DbSet<ChallengeParticipation> ChallengeParticipations { get; set; }
        public DbSet<RewardPoint> RewardPoints { get; set; }
        public DbSet<WorkoutCompletion> WorkoutCompletions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TrainerRequest>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TrainerRequest>()
                .HasOne(r => r.Trainer)
                .WithMany()
                .HasForeignKey(r => r.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Member)
                .WithMany(m => m.Payments)
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Membership)
                .WithMany()
                .HasForeignKey(p => p.MembershipId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Membership>()
                .HasOne(m => m.Member)
                .WithMany(m => m.Memberships)
                .HasForeignKey(m => m.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MembershipApplication>()
                .HasOne(a => a.Member)
                .WithMany(m => m.MembershipApplications)
                .HasForeignKey(a => a.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Member)
                .WithMany()
                .HasForeignKey(a => a.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkoutProfile>()
                .HasOne(w => w.Member)
                .WithMany(m => m.WorkoutProfiles)
                .HasForeignKey(w => w.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkoutPlan>()
                .HasOne(w => w.Member)
                .WithMany(m => m.WorkoutPlans)
                .HasForeignKey(w => w.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MealPlan>()
                .HasOne(m => m.Member)
                .WithMany(m => m.MealPlans)
                .HasForeignKey(m => m.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MealPlanItem>()
                .HasOne(m => m.MealPlan)
                .WithMany(m => m.MealPlanItems)
                .HasForeignKey(m => m.MealPlanId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FitnessChallenge>()
                .HasOne(c => c.Member)
                .WithMany(m => m.FitnessChallenges)
                .HasForeignKey(c => c.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChallengeParticipation>()
                .HasOne(p => p.FitnessChallenge)
                .WithOne(c => c.Participation)
                .HasForeignKey<ChallengeParticipation>(p => p.FitnessChallengeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChallengeParticipation>()
                .HasOne(p => p.Member)
                .WithMany(m => m.ChallengeParticipations)
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RewardPoint>()
                .HasOne(r => r.Member)
                .WithMany(m => m.RewardPoints)
                .HasForeignKey(r => r.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkoutCompletion>()
                .HasOne(w => w.Member)
                .WithMany(m => m.WorkoutCompletions)
                .HasForeignKey(w => w.MemberId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}