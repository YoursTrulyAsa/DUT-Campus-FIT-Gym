using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Student,Staff")]
    public class WorkoutController : Controller
    {
        private readonly GymDbContext _context;
        private readonly RewardService _rewardService;

        public WorkoutController(
            GymDbContext context,
            RewardService rewardService)
        {
            _context = context;
            _rewardService = rewardService;
        }

        [HttpGet]
        public async Task<IActionResult> MyWorkout()
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var programme = await _context.WorkoutProgrammes
                .Include(p => p.Member)
                .Where(p =>
                    p.MemberId == memberId &&
                    !p.IsCompleted &&
                    _context.WorkoutPlans.Any(w =>
                        w.MemberId == memberId &&
                        w.WorkoutProgrammeId == p.WorkoutProgrammeId))
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (programme == null)
            {
                var completedProgramme =
                    await _context.WorkoutProgrammes
                        .Where(p =>
                            p.MemberId == memberId &&
                            p.IsCompleted)
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefaultAsync();

                if (completedProgramme != null)
                {
                    ViewBag.CompletedProgramme = completedProgramme;
                }

                return View(new List<WorkoutPlan>());
            }

            var workouts = await _context.WorkoutPlans
                .Include(w => w.Exercise)
                .Where(w =>
                    w.MemberId == memberId &&
                    w.WorkoutProgrammeId == programme.WorkoutProgrammeId)
                .OrderBy(w => w.WeekNumber)
                .ThenBy(w => w.WorkoutDay)
                .ThenBy(w => w.WorkoutPlanId)
                .ToListAsync();

            var completions = await _context.WorkoutCompletions
                .Where(c =>
                    c.MemberId == memberId &&
                    c.WorkoutProgrammeId ==
                        programme.WorkoutProgrammeId)
                .ToListAsync();

            ViewBag.WorkoutProgramme = programme;

            ViewBag.CompletedDays = completions
                .Select(c =>
                    $"{c.WeekNumber}|{c.WorkoutDay}")
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var totalWorkoutDays = workouts
                .GroupBy(w => new
                {
                    w.WeekNumber,
                    w.WorkoutDay
                })
                .Count();

            var completedWorkoutDays = completions
                .Select(c =>
                    $"{c.WeekNumber}|{c.WorkoutDay}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            ViewBag.TotalWorkoutDays = totalWorkoutDays;
            ViewBag.CompletedWorkoutDays = completedWorkoutDays;

            return View(workouts);
        }

        [HttpGet]
        public IActionResult Create()
        {
            LoadExercises();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(WorkoutPlan workout)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            workout.MemberId = memberId;

            var validLevels = new[]
            {
                "Beginner",
                "Intermediate",
                "Pro"
            };

            if (string.IsNullOrWhiteSpace(workout.Level))
            {
                workout.Level = "Beginner";
            }
            else
            {
                workout.Level = workout.Level.Trim();

                if (!validLevels.Contains(
                    workout.Level,
                    StringComparer.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        "Level",
                        "Please select a valid fitness level.");
                }
                else
                {
                    workout.Level =
                        validLevels.First(level =>
                            level.Equals(
                                workout.Level,
                                StringComparison.OrdinalIgnoreCase));
                }
            }

            if (workout.ExerciseId.HasValue)
            {
                var exerciseExists =
                    _context.Exercises.Any(e =>
                        e.ExerciseId ==
                        workout.ExerciseId.Value);

                if (!exerciseExists)
                {
                    ModelState.AddModelError(
                        "ExerciseId",
                        "Please select a valid exercise.");
                }
            }

            if (ModelState.IsValid)
            {
                _context.WorkoutPlans.Add(workout);
                _context.SaveChanges();

                return RedirectToAction(nameof(MyWorkout));
            }

            LoadExercises();
            return View(workout);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var workout = await _context.WorkoutPlans
                .Include(w => w.Exercise)
                .FirstOrDefaultAsync(w =>
                    w.WorkoutPlanId == id &&
                    w.MemberId == memberId);

            if (workout == null)
                return NotFound();

            LoadExercises();

            return View(workout);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(WorkoutPlan workout)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var existingWorkout =
                await _context.WorkoutPlans
                    .FirstOrDefaultAsync(w =>
                        w.WorkoutPlanId ==
                            workout.WorkoutPlanId &&
                        w.MemberId == memberId);

            if (existingWorkout == null)
                return NotFound();

            var validLevels = new[]
            {
                "Beginner",
                "Intermediate",
                "Pro"
            };

            if (string.IsNullOrWhiteSpace(workout.Level))
            {
                ModelState.AddModelError(
                    "Level",
                    "Please select a fitness level.");
            }
            else
            {
                workout.Level = workout.Level.Trim();

                if (!validLevels.Contains(
                    workout.Level,
                    StringComparer.OrdinalIgnoreCase))
                {
                    ModelState.AddModelError(
                        "Level",
                        "Please select a valid fitness level.");
                }
                else
                {
                    workout.Level =
                        validLevels.First(level =>
                            level.Equals(
                                workout.Level,
                                StringComparison.OrdinalIgnoreCase));
                }
            }

            if (workout.ExerciseId.HasValue)
            {
                var exerciseExists =
                    await _context.Exercises.AnyAsync(e =>
                        e.ExerciseId ==
                        workout.ExerciseId.Value);

                if (!exerciseExists)
                {
                    ModelState.AddModelError(
                        "ExerciseId",
                        "Please select a valid exercise.");
                }
            }

            if (!ModelState.IsValid)
            {
                LoadExercises();
                return View(workout);
            }

            existingWorkout.WorkoutName =
                workout.WorkoutName;

            existingWorkout.Level =
                workout.Level;

            existingWorkout.ExerciseId =
                workout.ExerciseId;

            existingWorkout.WorkoutDay =
                workout.WorkoutDay;

            existingWorkout.WeekNumber =
                workout.WeekNumber;

            existingWorkout.Sets =
                workout.Sets;

            existingWorkout.Repetitions =
                workout.Repetitions;

            existingWorkout.RestTime =
                workout.RestTime;

            existingWorkout.Description =
                workout.Description;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyWorkout));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var workout = await _context.WorkoutPlans
                .Include(w => w.Exercise)
                .FirstOrDefaultAsync(w =>
                    w.WorkoutPlanId == id &&
                    w.MemberId == memberId);

            if (workout == null)
                return NotFound();

            return View(workout);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var workout = await _context.WorkoutPlans
                .FirstOrDefaultAsync(w =>
                    w.WorkoutPlanId == id &&
                    w.MemberId == memberId);

            if (workout == null)
                return NotFound();

            _context.WorkoutPlans.Remove(workout);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(MyWorkout));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteWorkout(
    int workoutProgrammeId,
    int weekNumber,
    string workoutDay)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrWhiteSpace(workoutDay))
                return RedirectToAction(nameof(MyWorkout));

            workoutDay = workoutDay.Trim();

            var programme =
                await _context.WorkoutProgrammes
                    .FirstOrDefaultAsync(p =>
                        p.WorkoutProgrammeId ==
                            workoutProgrammeId &&
                        p.MemberId == memberId);

            if (programme == null)
                return NotFound();

            if (programme.IsCompleted)
            {
                TempData["WorkoutCompleted"] =
                    "This programme has already been completed.";

                return RedirectToAction(nameof(MyWorkout));
            }

            if (weekNumber < 1 || weekNumber > 4)
                return RedirectToAction(nameof(MyWorkout));

            var dayExists =
                await _context.WorkoutPlans.AnyAsync(w =>
                    w.WorkoutProgrammeId ==
                        programme.WorkoutProgrammeId &&
                    w.MemberId == memberId &&
                    w.WeekNumber == weekNumber &&
                    w.WorkoutDay == workoutDay);

            if (!dayExists)
                return RedirectToAction(nameof(MyWorkout));

            var alreadyCompleted =
                await _context.WorkoutCompletions.AnyAsync(c =>
                    c.WorkoutProgrammeId ==
                        programme.WorkoutProgrammeId &&
                    c.MemberId == memberId &&
                    c.WeekNumber == weekNumber &&
                    c.WorkoutDay == workoutDay);

            if (alreadyCompleted)
            {
                TempData["WorkoutCompleted"] =
                    "You have already completed this workout day.";

                return RedirectToAction(nameof(MyWorkout));
            }

            var rewardPoints =
                GetLevelReward(programme.FitnessLevel);

            var completion =
                new WorkoutCompletion
                {
                    WorkoutProgrammeId =
                        programme.WorkoutProgrammeId,

                    MemberId =
                        memberId,

                    WeekNumber =
                        weekNumber,

                    WorkoutDay =
                        workoutDay,

                    WorkoutName =
                        programme.ProgrammeName,

                    FitnessLevel =
                        programme.FitnessLevel,

                    CompletedAt =
                        DateTime.Now,

                    RewardPoints =
                        rewardPoints
                };

            _context.WorkoutCompletions.Add(completion);

            _context.RewardPoints.Add(
                new RewardPoint
                {
                    MemberId = memberId,
                    Points = rewardPoints,
                    Reason =
                        $"Completed Week {weekNumber} {workoutDay} workout: {programme.ProgrammeName}",
                    EarnedAt = DateTime.Now
                });

            await _context.SaveChangesAsync();

            var totalWorkoutDays =
                await _context.WorkoutPlans
                    .Where(w =>
                        w.WorkoutProgrammeId ==
                            programme.WorkoutProgrammeId &&
                        w.MemberId == memberId)
                    .Select(w => new
                    {
                        w.WeekNumber,
                        w.WorkoutDay
                    })
                    .Distinct()
                    .CountAsync();

            var completedWorkoutDays =
                await _context.WorkoutCompletions
                    .Where(c =>
                        c.WorkoutProgrammeId ==
                            programme.WorkoutProgrammeId &&
                        c.MemberId == memberId)
                    .Select(c => new
                    {
                        c.WeekNumber,
                        c.WorkoutDay
                    })
                    .Distinct()
                    .CountAsync();

            if (totalWorkoutDays > 0 &&
                completedWorkoutDays >= totalWorkoutDays)
            {
                programme.IsCompleted = true;

                await _context.SaveChangesAsync();

                TempData["WorkoutCompleted"] =
                    $"Workout completed! You earned {rewardPoints} reward points.";

                TempData["ProgrammeCompleted"] = "true";
                TempData["CompletedProgrammeId"] =
                    programme.WorkoutProgrammeId.ToString();
                TempData["CompletedFitnessLevel"] =
                    programme.FitnessLevel;
                TempData["CompletedProgrammeName"] =
                    programme.ProgrammeName;
            }
            else
            {
                TempData["WorkoutCompleted"] =
                    $"Workout completed! You earned {rewardPoints} reward points.";
            }

            return RedirectToAction(nameof(MyWorkout));
        }

        private int GetLevelReward(string level)
        {
            if (string.Equals(
                    level,
                    "Pro",
                    StringComparison.OrdinalIgnoreCase))
            {
                return 7;
            }

            if (string.Equals(
                    level,
                    "Intermediate",
                    StringComparison.OrdinalIgnoreCase))
            {
                return 5;
            }

            return 3;
        }

        private void LoadExercises()
        {
            ViewBag.Exercises =
                _context.Exercises
                    .OrderBy(e => e.Category)
                    .ThenBy(e => e.ExerciseName)
                    .ToList();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdvanceLevel()
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var profile =
                await _context.WorkoutProfiles
                    .FirstOrDefaultAsync(p => p.MemberId == memberId);

            if (profile == null)
                return RedirectToAction(
                    "Create",
                    "WorkoutProfile");

            if (string.Equals(
                    profile.FitnessLevel,
                    "Beginner",
                    StringComparison.OrdinalIgnoreCase))
            {
                profile.FitnessLevel = "Intermediate";
            }
            else if (string.Equals(
                    profile.FitnessLevel,
                    "Intermediate",
                    StringComparison.OrdinalIgnoreCase))
            {
                profile.FitnessLevel = "Pro";
            }
            else
            {
                TempData["WorkoutCompleted"] =
                    "You have reached the Pro level. Keep pushing your progress!";

                return RedirectToAction(nameof(MyWorkout));
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "AIWorkout",
                "FitnessAI");
        }
    }
}