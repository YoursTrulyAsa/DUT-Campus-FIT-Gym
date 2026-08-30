using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Filters;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Mvc;
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

        [ServiceFilter(typeof(ActiveMembershipFilter))]
        public IActionResult MyWorkout()
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            int id = int.Parse(memberId);

            var workouts = _context.WorkoutPlans
                .Where(w => w.MemberId == id)
                .ToList();

            return View(workouts);
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(WorkoutPlan workout)
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            workout.MemberId = int.Parse(memberId);

            if (ModelState.IsValid)
            {
                _context.WorkoutPlans.Add(workout);
                _context.SaveChanges();

                return RedirectToAction("MyWorkout");
            }

            return View(workout);
        }
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            int currentMemberId = int.Parse(memberId);

            var workout = _context.WorkoutPlans
                .FirstOrDefault(w => w.WorkoutPlanId == id &&
                                     w.MemberId == currentMemberId);

            if (workout == null)
            {
                return NotFound();
            }

            return View(workout);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(WorkoutPlan workout)
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            int currentMemberId = int.Parse(memberId);

            var existingWorkout = _context.WorkoutPlans
                .FirstOrDefault(w => w.WorkoutPlanId == workout.WorkoutPlanId &&
                                     w.MemberId == currentMemberId);

            if (existingWorkout == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                existingWorkout.WorkoutName = workout.WorkoutName;
                existingWorkout.ExerciseName = workout.ExerciseName;
                existingWorkout.WorkoutDay = workout.WorkoutDay;
                existingWorkout.Sets = workout.Sets;
                existingWorkout.Repetitions = workout.Repetitions;
                existingWorkout.RestTime = workout.RestTime;
                existingWorkout.Description = workout.Description;

                _context.SaveChanges();

                return RedirectToAction("MyWorkout");
            }

            return View(workout);
        }

        [HttpGet]
        public IActionResult Delete(int id)
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            int currentMemberId = int.Parse(memberId);

            var workout = _context.WorkoutPlans
                .FirstOrDefault(w => w.WorkoutPlanId == id &&
                                     w.MemberId == currentMemberId);

            if (workout == null)
            {
                return NotFound();
            }

            return View(workout);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            int currentMemberId = int.Parse(memberId);

            var workout = _context.WorkoutPlans
                .FirstOrDefault(w => w.WorkoutPlanId == id &&
                                     w.MemberId == currentMemberId);

            if (workout == null)
            {
                return NotFound();
            }

            _context.WorkoutPlans.Remove(workout);
            _context.SaveChanges();

            return RedirectToAction("MyWorkout");
        }

    }
}
