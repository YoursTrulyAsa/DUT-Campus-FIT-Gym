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
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Equipment> Equipment { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
    }
}
