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
        public DbSet<EquipmentPenalty> EquipmentPenalties { get; set; }
        public DbSet<Announcement> Announcements { get; set; }

        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<TrainerRequest> TrainerRequests { get; set; }

        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
        public DbSet<WorkoutProfile> WorkoutProfiles { get; set; }
        public DbSet<WorkoutProgramme> WorkoutProgrammes { get; set; }
        public DbSet<WorkoutCompletion> WorkoutCompletions { get; set; }
        public DbSet<WorkoutResult> WorkoutResults { get; set; }
        public DbSet<SavedWorkoutResult> SavedWorkoutResults { get; set; }

        public DbSet<BankDetails> BankingDetails { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public DbSet<MealPlan> MealPlans { get; set; }
        public DbSet<MealPlanItem> MealPlanItems { get; set; }

        public DbSet<FitnessChallenge> FitnessChallenges { get; set; }
        public DbSet<ChallengeParticipation> ChallengeParticipations { get; set; }
        public DbSet<ChallengeProof> ChallengeProofs { get; set; }
        public DbSet<RewardPoint> RewardPoints { get; set; }
        public DbSet<Exercise> Exercises { get; set; }
        public DbSet<WeightHistory> WeightHistories { get; set; }
        public DbSet<MonthlyLeaderboard> MonthlyLeaderboards { get; set; }
        public DbSet<TrainerBooking> TrainerBookings { get; set; }
        public DbSet<MemberReview> MemberReviews { get; set; }
        public DbSet<PrivateTrainerSubscription> PrivateTrainerSubscriptions { get; set; }

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

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.EquipmentPenalty)
                .WithMany()
                .HasForeignKey(p => p.EquipmentPenaltyId)
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

            modelBuilder.Entity<WorkoutPlan>()
                .HasOne(w => w.Exercise)
                .WithMany()
                .HasForeignKey(w => w.ExerciseId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkoutProgramme>()
                .HasOne(w => w.Member)
                .WithMany()
                .HasForeignKey(w => w.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkoutPlan>()
                .HasOne(w => w.WorkoutProgramme)
                .WithMany()
                .HasForeignKey(w => w.WorkoutProgrammeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkoutCompletion>()
                .HasOne(w => w.Member)
                .WithMany(m => m.WorkoutCompletions)
                .HasForeignKey(w => w.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WorkoutCompletion>()
                .HasOne(w => w.WorkoutProgramme)
                .WithMany()
                .HasForeignKey(w => w.WorkoutProgrammeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkoutCompletion>()
                .HasIndex(w => new
                {
                    w.WorkoutProgrammeId,
                    w.WeekNumber,
                    w.WorkoutDay
                })
                .IsUnique();

            modelBuilder.Entity<WorkoutResult>()
                .HasOne(w => w.Member)
                .WithMany()
                .HasForeignKey(w => w.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkoutResult>()
                .HasOne(w => w.WorkoutProgramme)
                .WithMany()
                .HasForeignKey(w => w.WorkoutProgrammeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SavedWorkoutResult>()
                .HasOne(s => s.Member)
                .WithMany()
                .HasForeignKey(s => s.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SavedWorkoutResult>()
                .HasOne(s => s.WorkoutResult)
                .WithMany()
                .HasForeignKey(s => s.WorkoutResultId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SavedWorkoutResult>()
                .HasIndex(s => new
                {
                    s.MemberId,
                    s.WorkoutResultId
                })
                .IsUnique();

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

            modelBuilder.Entity<WeightHistory>()
                .HasOne(w => w.Member)
                .WithMany()
                .HasForeignKey(w => w.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MonthlyLeaderboard>()
                .HasIndex(x => new
                {
                    x.Year,
                    x.Month,
                    x.MemberId
                })
                .IsUnique();

            modelBuilder.Entity<MonthlyLeaderboard>()
                .HasOne(x => x.Member)
                .WithMany()
                .HasForeignKey(x => x.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EquipmentPenalty>()
                .HasOne(p => p.Member)
                .WithMany()
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EquipmentPenalty>()
                .HasOne(p => p.Reservation)
                .WithMany()
                .HasForeignKey(p => p.ReservationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MemberReview>()
                .HasOne(r => r.Member)
                .WithMany()
                .HasForeignKey(r => r.MemberId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChallengeProof>()
                .HasOne(p => p.FitnessChallenge)
                .WithMany()
                .HasForeignKey(p => p.FitnessChallengeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChallengeProof>()
                .HasOne(p => p.Member)
                .WithMany()
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChallengeProof>()
                .HasOne(p => p.ReviewedByTrainer)
                .WithMany()
                .HasForeignKey(p => p.ReviewedByTrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PrivateTrainerSubscription>()
                .HasOne(s => s.Member)
                .WithMany()
                .HasForeignKey(s => s.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PrivateTrainerSubscription>()
                .HasOne(s => s.Trainer)
                .WithMany()
                .HasForeignKey(s => s.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.TrainerBooking)
                .WithMany()
                .HasForeignKey(p => p.TrainerBookingId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}