using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DUT_Campus_FIT_Gym.Controllers
{
    public class WorkoutController : Controller
    {

        private readonly GymDbContext _context;

        public WorkoutController(GymDbContext context)
        {
            _context = context;
        }

        // Shows the workout profile form
        [HttpGet]
        public IActionResult CreateProfile()
        {
            var memberIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim.Value);

            ViewBag.MemberId = memberId;

            return View();
        }

        // Saves workout profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProfile(WorkoutProfile profile)
        {
            var memberIdClaim = User.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim.Value);

            // Make sure the profile belongs to the logged-in member
            profile.MemberId = memberId;

            if (!ModelState.IsValid)
            {
                ViewBag.MemberId = memberId;
                return View(profile);
            }

            // Check that the member actually exists
            var memberExists = await _context.Members
                .AnyAsync(m => m.MemberId == memberId);

            if (!memberExists)
            {
                return NotFound("The logged-in member could not be found.");
            }

            // Check if the member already has a workout profile
            var existingProfile = await _context.WorkoutProfiles
                .FirstOrDefaultAsync(p => p.MemberId == memberId);

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

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Generate",
                new { memberId = memberId }
            );
        }

        // Generates a basic workout plan
        public async Task<IActionResult> Generate(int memberId)
        {
            var profile = await _context.WorkoutProfiles
                .FirstOrDefaultAsync(x => x.MemberId == memberId);

            if (profile == null)
            {
                return RedirectToAction("CreateProfile");
            }

            // Remove old generated plan
            var oldPlans = await _context.WorkoutPlans
                .Where(x => x.MemberId == memberId)
                .ToListAsync();

            _context.WorkoutPlans.RemoveRange(oldPlans);

            // Create a basic plan based on goal
            var workoutPlans = new List<WorkoutPlan>();

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
                    Description = "Perform controlled squats with good form."
                });

                workoutPlans.Add(new WorkoutPlan
                {
                    MemberId = memberId,
                    WorkoutName = "Strength Workout",
                    ExerciseName = "Push-ups",
                    WorkoutDay = "Wednesday",
                    Sets = 3,
                    Repetitions = 10,
                    RestTime = 60,
                    Description = "Perform controlled push-ups with good form."
                });

                workoutPlans.Add(new WorkoutPlan
                {
                    MemberId = memberId,
                    WorkoutName = "Strength Workout",
                    ExerciseName = "Lunges",
                    WorkoutDay = "Friday",
                    Sets = 3,
                    Repetitions = 10,
                    RestTime = 60,
                    Description = "Perform controlled alternating lunges."
                });
            }
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
            else if (profile.Goal == "Weight Loss")
            {
                workoutPlans.Add(new WorkoutPlan
                {
                    MemberId = memberId,
                    WorkoutName = "Fitness & Activity Plan",
                    ExerciseName = "Walking",
                    WorkoutDay = "Monday",
                    Sets = 1,
                    Repetitions = 20,
                    RestTime = 0,
                    Description = "20-minute moderate walking session."
                });

                workoutPlans.Add(new WorkoutPlan
                {
                    MemberId = memberId,
                    WorkoutName = "Fitness & Activity Plan",
                    ExerciseName = "Bodyweight Squats",
                    WorkoutDay = "Wednesday",
                    Sets = 3,
                    Repetitions = 10,
                    RestTime = 60,
                    Description = "Perform controlled bodyweight squats."
                });

                workoutPlans.Add(new WorkoutPlan
                {
                    MemberId = memberId,
                    WorkoutName = "Fitness & Activity Plan",
                    ExerciseName = "Walking",
                    WorkoutDay = "Friday",
                    Sets = 1,
                    Repetitions = 25,
                    RestTime = 0,
                    Description = "25-minute moderate walking session."
                });
            }
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
                    ExerciseName = "Walking",
                    WorkoutDay = "Wednesday",
                    Sets = 1,
                    Repetitions = 15,
                    RestTime = 0,
                    Description = "15-minute walking session."
                });
            }

            _context.WorkoutPlans.AddRange(workoutPlans);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Plan",
                new { memberId = memberId }
            );
        }

        // Displays the workout plan
        public async Task<IActionResult> Plan(int memberId)
        {
            var plans = await _context.WorkoutPlans
                .Where(x => x.MemberId == memberId)
                .OrderBy(x => x.WorkoutDay)
                .ToListAsync();

            return View(plans);
        }
        public IActionResult Index()
        {
            var memberIdClaim = User.FindFirstValue(
         ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            return RedirectToAction(
                "WorkoutPlan",
                "Member");
        }
    }
}
