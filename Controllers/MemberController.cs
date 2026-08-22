using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Student")]
    public class MemberController : Controller
    {
        private readonly GymDbContext _context;

        public MemberController(GymDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId);

            if (member == null)
            {
                return NotFound();
            }

            var membership = _context.Memberships
                .FirstOrDefault(m => m.MemberId == memberId);

            var attendanceCount = _context.Attendances
                .Count(a => a.MemberId == memberId);

            var dashboardData = new
            {
                Member = member,
                Membership = membership,
                AttendanceCount = attendanceCount
            };

            return View(dashboardData);
        }

        public IActionResult Profile()
        {
            var memberIdCalim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

            if (memberIdCalim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdCalim);

            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId);

            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        public IActionResult Membership()
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var membership = _context.Memberships
                .FirstOrDefault(m => m.MemberId == memberId);

            if (membership == null)
            {
                return NotFound("No membership found for this member.");
            }

            return View(membership);
        }

        public IActionResult Attendance()
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var attendance = _context.Attendances
                .Where(a => a.MemberId == memberId)
                .OrderByDescending(a => a.CheckInTime)
                .ToList();

            return View(attendance);
        }

        public IActionResult Equipment()
        {
            var equipment = _context.Equipment.ToList();

            return View(equipment);
        }

        public IActionResult Reservations()
        {
            var memberId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
            );

            var reservations = _context.Reservations
                .Where(r => r.MemberID == memberId)
                .Join(
                    _context.Equipment,
                    reservation => reservation.EquipmentID,
                    equipment => equipment.EquipmentID,
                    (reservation, equipment) => new ReservationViewModel
                    {
                        ReservationID = reservation.ReservationID,
                        EquipmentName = equipment.EquipmentName,
                        ReservationDate = reservation.ReservationDate,
                        Status = reservation.Status
                    }
                )
                .ToList();

            return View(reservations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApplyMembership(string membershipType)
        {
            var memberIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var existingMembership = _context.Memberships
                .FirstOrDefault(m => m.MemberId == memberId);

            if (existingMembership != null)
            {
                return View("ApplyMembership", existingMembership);
            }

            var membership = new Membership
            {
                MemberId = memberId,
                MembershipType = membershipType,
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(1),
                Status = "Pending",
                Price = 0
            };

            _context.Memberships.Add(membership);
            _context.SaveChanges();

            return RedirectToAction("Membership");

        }
        [HttpGet]
        public IActionResult ApplyMembership()
        {
            return View();
        }

        public async Task<IActionResult> WorkoutPlan()
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var plans = await _context.WorkoutPlans
                .Where(w => w.MemberId == memberId)
                .OrderBy(w => w.WorkoutDay)
                .ToListAsync();

            if (!plans.Any())
            {
                return RedirectToAction(
                    "CreateProfile",
                    "Workout",
                    new { memberId = memberId });
            }

            return View(plans);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateProfile(WorkoutProfile profile)
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            // Always use the logged-in member
            profile.MemberId = memberId;

            if (!ModelState.IsValid)
            {
                return View(profile);
            }

            var existingProfile = _context.WorkoutProfiles
                .FirstOrDefault(p => p.MemberId == memberId);

            if (existingProfile != null)
            {
                existingProfile.Age = profile.Age;
                existingProfile.Weight = profile.Weight;
                existingProfile.Height = profile.Height;
                existingProfile.Goal = profile.Goal;
            }
            else
            {
                _context.WorkoutProfiles.Add(profile);
            }

            _context.SaveChanges();

            return RedirectToAction("Generate");
        }

        [HttpGet]
        public IActionResult Generate()
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var profile = _context.WorkoutProfiles
                .FirstOrDefault(p => p.MemberId == memberId);

            if (profile == null)
            {
                return RedirectToAction("CreateProfile");
            }

            // Remove previous workout plan
            var oldPlans = _context.WorkoutPlans
                .Where(w => w.MemberId == memberId)
                .ToList();

            _context.WorkoutPlans.RemoveRange(oldPlans);

            var workoutPlans = new List<WorkoutPlan>();

            // STRENGTH
            if (profile.Goal == "Strength")
            {
                workoutPlans.Add(new WorkoutPlan
                {
                    MemberId = memberId,
                    WorkoutName = "Strength Workout",
                    ExerciseName = "Squats",
                    WorkoutDay = "Monday",
                    Sets = 3,
                    Repetitions = 10,
                    RestTime = 60,
                    Description = "Perform controlled bodyweight squats."
                });

                workoutPlans.Add(new WorkoutPlan
                {
                    MemberId = memberId,
                    WorkoutName = "Strength Workout",
                    ExerciseName = "Push-ups",
                    WorkoutDay = "Monday",
                    Sets = 3,
                    Repetitions = 10,
                    RestTime = 60,
                    Description = "Keep your body straight while performing push-ups."
                });

                workoutPlans.Add(new WorkoutPlan
                {
                    MemberId = memberId,
                    WorkoutName = "Strength Workout",
                    ExerciseName = "Lunges",
                    WorkoutDay = "Wednesday",
                    Sets = 3,
                    Repetitions = 10,
                    RestTime = 60,
                    Description = "Perform controlled alternating lunges."
                });

                workoutPlans.Add(new WorkoutPlan
                {
                    MemberId = memberId,
                    WorkoutName = "Strength Workout",
                    ExerciseName = "Plank",
                    WorkoutDay = "Friday",
                    Sets = 3,
                    Repetitions = 30,
                    RestTime = 60,
                    Description = "Hold the plank position while maintaining good form."
                });
            }

            // FITNESS
            else if (profile.Goal == "Fitness")
            {
                workoutPlans.Add(new WorkoutPlan
                {
                    MemberId = memberId,
                    WorkoutName = "General Fitness",
                    ExerciseName = "Bodyweight Squats",
                    WorkoutDay = "Monday",
                    Sets = 3,
                    Repetitions = 12,
                    RestTime = 60,
                    Description = "Perform controlled bodyweight squats."
                });

                workoutPlans.Add(new WorkoutPlan
                {
                    MemberId = memberId,
                    WorkoutName = "General Fitness",
                    ExerciseName = "Walking",
                    WorkoutDay = "Wednesday",
                    Sets = 1,
                    Repetitions = 20,
                    RestTime = 0,
                    Description = "20-minute walking session."
                });

                workoutPlans.Add(new WorkoutPlan
                {
                    MemberId = memberId,
                    WorkoutName = "General Fitness",
                    ExerciseName = "Push-ups",
                    WorkoutDay = "Friday",
                    Sets = 3,
                    Repetitions = 10,
                    RestTime = 60,
                    Description = "Perform controlled push-ups."
                });
            }

            // DEFAULT
            else
            {
                workoutPlans.Add(new WorkoutPlan
                {
                    MemberId = memberId,
                    WorkoutName = "Beginner Workout",
                    ExerciseName = "Bodyweight Squats",
                    WorkoutDay = "Monday",
                    Sets = 2,
                    Repetitions = 10,
                    RestTime = 60,
                    Description = "Basic beginner exercise."
                });

                workoutPlans.Add(new WorkoutPlan
                {
                    MemberId = memberId,
                    WorkoutName = "Beginner Workout",
                    ExerciseName = "Push-ups",
                    WorkoutDay = "Wednesday",
                    Sets = 2,
                    Repetitions = 8,
                    RestTime = 60,
                    Description = "Basic upper-body exercise."
                });
            }

            _context.WorkoutPlans.AddRange(workoutPlans);

            _context.SaveChanges();

            return RedirectToAction("Plan");
        }
        [HttpGet]
        public IActionResult Plan()
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var plans = _context.WorkoutPlans
                .Where(w => w.MemberId == memberId)
                .OrderBy(w => w.WorkoutDay)
                .ToList();

            return View(plans);
        }

    }
}