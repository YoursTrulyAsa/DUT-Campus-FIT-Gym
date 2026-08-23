using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DUT_Campus_FIT_Gym.Controllers
{
    public class TrainerController : Controller
    {
        private readonly GymDbContext _context;

        public TrainerController(GymDbContext context)
        {
            _context = context;
        }

        // Trainer Dashboard
        public IActionResult Index()
        {
            return View();
        }

        // View all equipment
        public async Task<IActionResult> Equipment()
        {
            var equipment = await _context.Equipment.ToListAsync();
            return View(equipment);
        }

        // Add equipment
        [HttpGet]
        public IActionResult AddEquipment()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEquipment(Equipment equipment)
        {
            if (ModelState.IsValid)
            {
                _context.Equipment.Add(equipment);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Equipment));
            }

            return View(equipment);
        }

        public async Task<IActionResult> Workouts()
        {
            var workouts = await _context.WorkoutPlans
                .Include(w => w.Member)
                .ToListAsync();

            return View(workouts);
        }

        // GET: Create Workout
        [HttpGet]
        public IActionResult CreateWorkout()
        {
            var members = _context.Members
                .Where(m => m.Role == "Student" || m.Role == "Staff")
                .ToList();

            ViewBag.Members = members;

            return View();
        }

        // POST: Create Workout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateWorkout(WorkoutPlan workout)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Members = _context.Members
                    .Where(m => m.Role == "Student" || m.Role == "Staff")
                    .ToList();

                return View(workout);
            }

            var memberExists = _context.Members
                .Any(m => m.MemberId == workout.MemberId);

            if (!memberExists)
            {
                ModelState.AddModelError(
                    "MemberId",
                    "Please select a valid member.");

                ViewBag.Members = _context.Members
                    .Where(m => m.Role == "Student" || m.Role == "Staff")
                    .ToList();

                return View(workout);
            }

            _context.WorkoutPlans.Add(workout);
            _context.SaveChanges();

            TempData["Success"] =
                "Workout plan assigned successfully.";

            return RedirectToAction("Workouts");
        }
    }
}