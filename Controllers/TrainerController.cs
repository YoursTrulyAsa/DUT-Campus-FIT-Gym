using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class TrainerController : Controller
    {
        private readonly GymDbContext _context;

        public TrainerController(GymDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // TRAINER DASHBOARD
        // =========================================================

        public async Task<IActionResult> Index()
        {
            var trainerEmail =
                User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(trainerEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t =>
                    t.Email == trainerEmail);

            if (trainer == null)
            {
                return NotFound("Trainer account was not found.");
            }

            // Current active request
            var activeRequest = await _context.TrainerRequests
     .Include(r => r.Student)
         .ThenInclude(s => s.WorkoutProfiles)
     .Include(r => r.Student)
         .ThenInclude(s => s.WorkoutPlans)
     .FirstOrDefaultAsync(r =>
         r.TrainerId == trainer.TrainerId &&
         r.Status == "Accepted");

            // Number of pending requests
            var pendingRequests = await _context.TrainerRequests
                .CountAsync(r =>
                    r.TrainerId == trainer.TrainerId &&
                    r.Status == "Pending");

            ViewBag.ActiveRequest = activeRequest;
            ViewBag.PendingRequests = pendingRequests;

            if (activeRequest != null)
            {
                var profile = await _context.WorkoutProfiles
                    .FirstOrDefaultAsync(p =>
                        p.MemberId == activeRequest.Student.MemberId);

                var workoutPlans = await _context.WorkoutPlans
                    .Where(w =>
                        w.MemberId == activeRequest.Student.MemberId)
                    .ToListAsync();

                ViewBag.StudentProfile = profile;
                ViewBag.StudentWorkoutPlans = workoutPlans;
            }

            return View();
        }


        // =========================================================
        // EQUIPMENT
        // =========================================================

        public async Task<IActionResult> Equipment()
        {
            var equipment =
                await _context.Equipment
                    .OrderBy(e => e.EquipmentName)
                    .ToListAsync();

            return View(equipment);
        }


        // =========================================================
        // ADD EQUIPMENT
        // =========================================================

        [HttpGet]
        public IActionResult AddEquipment()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEquipment(
            Equipment equipment)
        {
            if (!ModelState.IsValid)
            {
                return View(equipment);
            }

            _context.Equipment.Add(equipment);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Equipment added successfully.";

            return RedirectToAction(nameof(Equipment));
        }


        // =========================================================
        // WORKOUTS
        // =========================================================

        public async Task<IActionResult> Workouts()
        {
            var workouts = await _context.WorkoutPlans
                .Include(w => w.Member)
                .OrderByDescending(w => w.WorkoutPlanId)
                .ToListAsync();

            return View(workouts);
        }

        // =========================================================
        // STUDENT WORKOUT PROFILES
        // =========================================================

        public async Task<IActionResult> StudentProfiles()
        {
            var students = await _context.Members
                .Where(m =>
                    m.Role == "Student" ||
                    m.Role == "Staff")
                .Include(m => m.WorkoutProfiles)
                .OrderBy(m => m.Name)
                .ThenBy(m => m.Surname)
                .ToListAsync();

            return View(students);
        }


        // =========================================================
        // VIEW STUDENT WORKOUT PROFILE
        // =========================================================

        public async Task<IActionResult> ViewProfile(int id)
        {
            var student = await _context.Members
                .Include(m => m.WorkoutProfiles)
                .Include(m => m.WorkoutPlans)
                .FirstOrDefaultAsync(m =>
                    m.MemberId == id &&
                    (m.Role == "Student" ||
                     m.Role == "Staff"));

            if (student == null)
            {
                return NotFound("Student was not found.");
            }

            return View(student);
        }


        // =========================================================
        // CREATE WORKOUT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> CreateWorkout()
        {
            var members = await _context.Members
                .Where(m =>
                    m.Role == "Student" ||
                    m.Role == "Staff")
                .ToListAsync();

            ViewBag.Members = members;

            return View();
        }


        // =========================================================
        // SAVE WORKOUT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWorkout(
            WorkoutPlan workout)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Members = await _context.Members
                    .Where(m =>
                        m.Role == "Student" ||
                        m.Role == "Staff")
                    .ToListAsync();

                return View(workout);
            }

            var memberExists = await _context.Members
                .AnyAsync(m =>
                    m.MemberId == workout.MemberId &&
                    (m.Role == "Student" ||
                     m.Role == "Staff"));

            if (!memberExists)
            {
                ModelState.AddModelError(
                    "MemberId",
                    "Please select a valid member.");

                ViewBag.Members = await _context.Members
                    .Where(m =>
                        m.Role == "Student" ||
                        m.Role == "Staff")
                    .ToListAsync();

                return View(workout);
            }

            _context.WorkoutPlans.Add(workout);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Workout plan assigned successfully.";

            return RedirectToAction(nameof(Workouts));
        }


        // =========================================================
        // TRAINER REQUESTS
        // =========================================================

        public async Task<IActionResult> Requests()
        {
            var trainerEmail =
                User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(trainerEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t =>
                    t.Email == trainerEmail);

            if (trainer == null)
            {
                return NotFound(
                    "Trainer account was not found.");
            }

            // Current active request
            var activeRequest =
                await _context.TrainerRequests
                    .Include(r => r.Student)
                    .FirstOrDefaultAsync(r =>
                        r.TrainerId == trainer.TrainerId &&
                        r.Status == "Accepted");

            // All requests belonging to this trainer
            var requests =
                await _context.TrainerRequests
                    .Include(r => r.Student)
                    .Where(r =>
                        r.TrainerId == trainer.TrainerId)
                    .OrderByDescending(r => r.RequestDate)
                    .ToListAsync();

            ViewBag.ActiveRequest = activeRequest;

            return View(requests);
        }


        // =========================================================
        // ACCEPT REQUEST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var trainerEmail =
                User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(trainerEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t =>
                    t.Email == trainerEmail);

            if (trainer == null)
            {
                return NotFound(
                    "Trainer account was not found.");
            }


            // IMPORTANT:
            // Check whether trainer already has an active workout.

            var activeRequest =
                await _context.TrainerRequests
                    .FirstOrDefaultAsync(r =>
                        r.TrainerId == trainer.TrainerId &&
                        r.Status == "Accepted");

            if (activeRequest != null)
            {
                TempData["Error"] =
                    "You already have an active workout. Complete it before accepting another request.";

                return RedirectToAction(nameof(Requests));
            }


            // Find the requested student request

            var request =
                await _context.TrainerRequests
                    .FirstOrDefaultAsync(r =>
                        r.TrainerRequestId == id &&
                        r.TrainerId == trainer.TrainerId);

            if (request == null)
            {
                TempData["Error"] =
                    "Trainer request was not found.";

                return RedirectToAction(nameof(Requests));
            }


            // Make sure request is still pending

            if (request.Status != "Pending")
            {
                TempData["Error"] =
                    "This request is no longer pending.";

                return RedirectToAction(nameof(Requests));
            }


            // Accept request

            request.Status = "Accepted";
            request.ResponseDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Trainer request accepted. Complete this workout before accepting another request.";

            return RedirectToAction(nameof(Requests));
        }


        // =========================================================
        // REJECT REQUEST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var trainerEmail =
                User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(trainerEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t =>
                    t.Email == trainerEmail);

            if (trainer == null)
            {
                return NotFound(
                    "Trainer account was not found.");
            }


            var request =
                await _context.TrainerRequests
                    .FirstOrDefaultAsync(r =>
                        r.TrainerRequestId == id &&
                        r.TrainerId == trainer.TrainerId);

            if (request == null)
            {
                TempData["Error"] =
                    "Trainer request was not found.";

                return RedirectToAction(nameof(Requests));
            }


            if (request.Status != "Pending")
            {
                TempData["Error"] =
                    "Only pending requests can be rejected.";

                return RedirectToAction(nameof(Requests));
            }


            request.Status = "Rejected";
            request.ResponseDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Trainer request rejected.";

            return RedirectToAction(nameof(Requests));
        }


        // =========================================================
        // COMPLETE WORKOUT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRequest(int id)
        {
            var trainerEmail =
                User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(trainerEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t =>
                    t.Email == trainerEmail);

            if (trainer == null)
            {
                return NotFound(
                    "Trainer account was not found.");
            }


            // Find the active request belonging to this trainer

            var request =
                await _context.TrainerRequests
                    .FirstOrDefaultAsync(r =>
                        r.TrainerRequestId == id &&
                        r.TrainerId == trainer.TrainerId &&
                        r.Status == "Accepted");

            if (request == null)
            {
                TempData["Error"] =
                    "Active workout was not found.";

                return RedirectToAction(nameof(Requests));
            }


            // Mark workout as completed

            request.Status = "Completed";
            request.ResponseDate = DateTime.Now;
            request.CompletionDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Workout completed successfully. You can now accept another student request.";

            return RedirectToAction(nameof(Requests));
        }
    }
}