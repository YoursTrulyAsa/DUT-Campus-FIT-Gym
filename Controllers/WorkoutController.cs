using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public IActionResult MyWorkout()
        {
            var memberId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            int id =
                int.Parse(memberId);

            var workouts =
                _context.WorkoutPlans
                    .Where(w =>
                        w.MemberId == id)
                    .ToList();

            var today =
                DateTime.Today;

            var tomorrow =
                today.AddDays(1);

            var completedToday =
                _context.WorkoutCompletions
                    .Where(w =>
                        w.MemberId == id &&
                        w.CompletedAt >= today &&
                        w.CompletedAt < tomorrow)
                    .Select(w =>
                        w.WorkoutName)
                    .ToHashSet();

            ViewBag.CompletedWorkouts =
                completedToday;

            return View(workouts);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            WorkoutPlan workout)
        {
            var memberId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            workout.MemberId =
                int.Parse(memberId);

            var validLevels =
                new[]
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

                    return View(workout);
                }

                workout.Level =
                    validLevels.First(
                        level =>
                            level.Equals(
                                workout.Level,
                                StringComparison.OrdinalIgnoreCase));
            }

            if (ModelState.IsValid)
            {
                _context.WorkoutPlans.Add(workout);
                _context.SaveChanges();

                return RedirectToAction(
                    "MyWorkout");
            }

            return View(workout);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var memberId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            int currentMemberId =
                int.Parse(memberId);

            var workout =
                _context.WorkoutPlans
                    .FirstOrDefault(w =>
                        w.WorkoutPlanId == id &&
                        w.MemberId == currentMemberId);

            if (workout == null)
            {
                return NotFound();
            }

            return View(workout);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            WorkoutPlan workout)
        {
            var memberId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            int currentMemberId =
                int.Parse(memberId);

            var existingWorkout =
                _context.WorkoutPlans
                    .FirstOrDefault(w =>
                        w.WorkoutPlanId == workout.WorkoutPlanId &&
                        w.MemberId == currentMemberId);

            if (existingWorkout == null)
            {
                return NotFound();
            }

            var validLevels =
                new[]
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
                workout.Level =
                    workout.Level.Trim();

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
                        validLevels.First(
                            level =>
                                level.Equals(
                                    workout.Level,
                                    StringComparison.OrdinalIgnoreCase));
                }
            }

            if (ModelState.IsValid)
            {
                existingWorkout.WorkoutName =
                    workout.WorkoutName;

                existingWorkout.Level =
                    workout.Level;

                existingWorkout.ExerciseName =
                    workout.ExerciseName;

                existingWorkout.WorkoutDay =
                    workout.WorkoutDay;

                existingWorkout.Sets =
                    workout.Sets;

                existingWorkout.Repetitions =
                    workout.Repetitions;

                existingWorkout.RestTime =
                    workout.RestTime;

                existingWorkout.Description =
                    workout.Description;

                _context.SaveChanges();

                return RedirectToAction(
                    "MyWorkout");
            }

            return View(workout);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var memberId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            int currentMemberId =
                int.Parse(memberId);

            var workout =
                _context.WorkoutPlans
                    .FirstOrDefault(w =>
                        w.WorkoutPlanId == id &&
                        w.MemberId == currentMemberId);

            if (workout == null)
            {
                return NotFound();
            }

            return View(workout);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(
            int id)
        {
            var memberId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            int currentMemberId =
                int.Parse(memberId);

            var workout =
                _context.WorkoutPlans
                    .FirstOrDefault(w =>
                        w.WorkoutPlanId == id &&
                        w.MemberId == currentMemberId);

            if (workout == null)
            {
                return NotFound();
            }

            _context.WorkoutPlans.Remove(workout);
            _context.SaveChanges();

            return RedirectToAction(
                "MyWorkout");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CompleteWorkout(
            string workoutName)
        {
            var memberId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (string.IsNullOrWhiteSpace(workoutName))
            {
                return RedirectToAction(
                    "MyWorkout");
            }

            int currentMemberId =
                int.Parse(memberId);

            workoutName =
                workoutName.Trim();

            var workout =
                _context.WorkoutPlans
                    .FirstOrDefault(w =>
                        w.MemberId == currentMemberId &&
                        w.WorkoutName == workoutName);

            if (workout == null)
            {
                return RedirectToAction(
                    "MyWorkout");
            }

            var today =
                DateTime.Today;

            var tomorrow =
                today.AddDays(1);

            var alreadyCompletedToday =
                _context.WorkoutCompletions.Any(w =>
                    w.MemberId == currentMemberId &&
                    w.WorkoutName == workoutName &&
                    w.CompletedAt >= today &&
                    w.CompletedAt < tomorrow);

            if (alreadyCompletedToday)
            {
                TempData["WorkoutCompleted"] =
                    "You have already completed this workout today.";

                return RedirectToAction(
                    "MyWorkout");
            }

            var rewardPoints =
                _rewardService.GetLevelReward(
                    workout.Level);

            _context.WorkoutCompletions.Add(
                new WorkoutCompletion
                {
                    MemberId =
                        currentMemberId,

                    WorkoutName =
                        workoutName,

                    CompletedAt =
                        DateTime.Now,

                    RewardPoints =
                        rewardPoints
                });

            _context.RewardPoints.Add(
                new RewardPoint
                {
                    MemberId =
                        currentMemberId,

                    Points =
                        rewardPoints,

                    Reason =
                        $"Completed {workout.Level} workout: {workoutName}",

                    EarnedAt =
                        DateTime.Now
                });

            _context.SaveChanges();

            TempData["WorkoutCompleted"] =
                $"Workout completed! You earned {rewardPoints} reward points.";

            return RedirectToAction(
                "MyWorkout");
        }
    }
}