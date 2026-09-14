using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Student,Staff")]
    public class FitnessInsightsController : Controller
    {
        private readonly GymDbContext _context;

        public FitnessInsightsController(GymDbContext context)
        {
            _context = context;
        }

        private int? GetMemberId()
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim))
            {
                return null;
            }

            if (!int.TryParse(memberIdClaim, out int memberId))
            {
                return null;
            }

            return memberId;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var attendance = _context.Attendances
                .Where(a => a.MemberId == memberId.Value)
                .OrderByDescending(a => a.CheckInTime)
                .ToList();

            var completedAttendance = attendance
                .Where(a => a.CheckOutTime.HasValue)
                .ToList();

            var totalGymMinutes = completedAttendance
                .Sum(a =>
                    (a.CheckOutTime!.Value - a.CheckInTime)
                    .TotalMinutes);

            var averageVisitMinutes = completedAttendance.Any()
                ? completedAttendance.Average(a =>
                    (a.CheckOutTime!.Value - a.CheckInTime)
                    .TotalMinutes)
                : 0;

            var today = DateTime.Today;

            var monday = today.AddDays(
                -((7 + (int)today.DayOfWeek - (int)DayOfWeek.Monday) % 7));

            var weeklyAttendance = attendance
                .Where(a =>
                    a.CheckInTime.Date >= monday &&
                    a.CheckInTime.Date <= today)
                .ToList();

            var weeklyGymMinutes = weeklyAttendance
                .Where(a => a.CheckOutTime.HasValue)
                .Sum(a =>
                    (a.CheckOutTime!.Value - a.CheckInTime)
                    .TotalMinutes);

            var workouts = _context.WorkoutPlans
                .Where(w => w.MemberId == memberId.Value)
                .ToList();

            var workoutDays = workouts
                .Select(w => w.WorkoutDay)
                .Where(d => !string.IsNullOrWhiteSpace(d))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            var workoutProfile = _context.WorkoutProfiles
                .FirstOrDefault(w =>
                    w.MemberId == memberId.Value);

            var membership = _context.Memberships
                .Where(m =>
                    m.MemberId == memberId.Value)
                .OrderByDescending(m =>
                    m.MembershipId)
                .FirstOrDefault();

            var activeMembership = _context.Memberships
                .Where(m =>
                    m.MemberId == memberId.Value &&
                    m.Status == "Active" &&
                    m.EndDate.HasValue &&
                    m.EndDate.Value.Date >= DateTime.Today)
                .OrderByDescending(m =>
                    m.MembershipId)
                .FirstOrDefault();

            var savedMealPlans = _context.MealPlans
                .Count(m =>
                    m.MemberId == memberId.Value);

            var totalRewardPoints = _context.RewardPoints
                .Where(r =>
                    r.MemberId == memberId.Value)
                .Sum(r => (int?)r.Points) ?? 0;

            var viewModel = new FitnessInsightsViewModel
            {
                FitnessGoal =
                    workoutProfile?.Goal ?? "Not set",

                Age =
                    workoutProfile?.Age ?? 0,

                Weight =
                    workoutProfile?.Weight ?? 0,

                Height =
                    workoutProfile?.Height ?? 0,

                MembershipStatus =
                    activeMembership?.Status ??
                    membership?.Status ??
                    "No Membership",

                MembershipType =
                    activeMembership?.MembershipType ??
                    membership?.MembershipType ??
                    "",

                MembershipDaysRemaining =
                    activeMembership?.EndDate.HasValue == true
                        ? Math.Max(
                            0,
                            (activeMembership.EndDate.Value.Date -
                             DateTime.Today).Days)
                        : 0,

                TotalGymVisits =
                    attendance.Count,

                CompletedVisits =
                    completedAttendance.Count,

                TotalGymHours =
                    totalGymMinutes / 60,

                AverageVisitMinutes =
                    averageVisitMinutes,

                WeeklyVisits =
                    weeklyAttendance.Count,

                WeeklyGymHours =
                    weeklyGymMinutes / 60,

                SavedWorkoutExercises =
                    workouts.Count,

                WorkoutDays =
                    workoutDays,

                TotalSets =
                    workouts.Sum(w => w.Sets),

                TotalRepetitions =
                    workouts.Sum(w => w.Repetitions),

                SavedMealPlans =
                    savedMealPlans,

                TotalRewardPoints =
                    totalRewardPoints
            };

            return View(viewModel);
        }
    }
}