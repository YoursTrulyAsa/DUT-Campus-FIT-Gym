using DUT_Campus_FIT_Gym.Models;
using Microsoft.EntityFrameworkCore;

namespace DUT_Campus_FIT_Gym.Data
{
    public class GymDbContext : DbContext
    {
        public GymDbContext(
            DbContextOptions<GymDbContext> options)
            : base(options)
        {
        }

        public DbSet<Member> Members { get; set; }

        public DbSet<Membership> Memberships { get; set; }

        public DbSet<Attendance> Attendances { get; set; }

        public DbSet<Equipment> Equipment { get; set; }

        public DbSet<Reservation> Reservations { get; set; }

        public DbSet<Announcement> Announcements { get; set; }

        public DbSet<Trainer> Trainers { get; set; }

        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }

        public DbSet<WorkoutProfile> WorkoutProfiles { get; set; }

        public DbSet<TrainerRequest> TrainerRequests { get; set; }
        public DbSet<MembershipApplication> MembershipApplications { get; set; }
        public DbSet<BankDetails> BankingDetails { get; set; }

        public DbSet<Payment> Payments { get; set; }


        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Student who sends the request
            modelBuilder.Entity<TrainerRequest>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Trainer is stored in the Members table
            modelBuilder.Entity<TrainerRequest>()
                .HasOne(r => r.Trainer)
                .WithMany()
                .HasForeignKey(r => r.TrainerId)
                .OnDelete(DeleteBehavior.Restrict);
            // =====================================================
            // PAYMENT RELATIONSHIPS
            // =====================================================

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Member)
                .WithMany()
                .HasForeignKey(p => p.MemberId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Membership)
                .WithMany()
                .HasForeignKey(p => p.MembershipId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}