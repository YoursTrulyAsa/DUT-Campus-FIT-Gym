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

            var activeRequest = await _context.TrainerRequests
                .Include(r => r.Student)
                    .ThenInclude(s => s.WorkoutProfiles)
                .Include(r => r.Student)
                    .ThenInclude(s => s.WorkoutPlans)
                        .ThenInclude(w => w.Exercise)
                .FirstOrDefaultAsync(r =>
                    r.TrainerId == trainer.TrainerId &&
                    r.Status == "Accepted");

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
                    .Include(w => w.Exercise)
                    .Where(w =>
                        w.MemberId == activeRequest.Student.MemberId)
                    .ToListAsync();

                ViewBag.StudentProfile = profile;
                ViewBag.StudentWorkoutPlans = workoutPlans;
            }

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
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

            return View(trainer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
    Trainer trainer,
    IFormFile? profilePicture)
        {
            var trainerEmail =
                User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(trainerEmail))
            {
                return RedirectToAction("Login", "Account");
            }

            var existingTrainer = await _context.Trainers
                .FirstOrDefaultAsync(t =>
                    t.Email == trainerEmail);

            if (existingTrainer == null)
            {
                return NotFound("Trainer account was not found.");
            }

            var validCategories = new[]
            {
        "General Fitness",
        "Strength Training",
        "Cardio & Endurance",
        "Weight Management",
        "Sports Training",
        "Functional Training"
    };

            if (!validCategories.Contains(trainer.Category))
            {
                ModelState.AddModelError(
                    "Category",
                    "Please select a valid trainer category.");

                trainer.TrainerId =
                    existingTrainer.TrainerId;

                trainer.TrainerName =
                    existingTrainer.TrainerName;

                trainer.Email =
                    existingTrainer.Email;

                trainer.ProfilePicture =
                    existingTrainer.ProfilePicture;

                return View(trainer);
            }

            if (profilePicture != null &&
                profilePicture.Length > 0)
            {
                var allowedExtensions = new[]
                {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

                var extension =
                    Path.GetExtension(profilePicture.FileName)
                        .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "ProfilePicture",
                        "Only JPG, JPEG, PNG and WEBP images are allowed.");

                    trainer.TrainerId =
                        existingTrainer.TrainerId;

                    trainer.TrainerName =
                        existingTrainer.TrainerName;

                    trainer.Email =
                        existingTrainer.Email;

                    trainer.ProfilePicture =
                        existingTrainer.ProfilePicture;

                    return View(trainer);
                }

                var uploadFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "trainers");

                Directory.CreateDirectory(uploadFolder);

                var fileName =
                    $"{Guid.NewGuid()}{extension}";

                var filePath =
                    Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await profilePicture.CopyToAsync(stream);
                }

                existingTrainer.ProfilePicture =
                    $"/uploads/trainers/{fileName}";
            }

            existingTrainer.Category =
                trainer.Category;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Trainer profile updated successfully.";

            return RedirectToAction(nameof(Profile));
        }

        public async Task<IActionResult> Exercises()
        {
            var exercises = await _context.Exercises
                .OrderBy(e => e.Category)
                .ThenBy(e => e.ExerciseName)
                .ToListAsync();

            return View(exercises);
        }

        [HttpGet]
        public IActionResult AddExercise()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExercise(Exercise exercise)
        {
            if (!ModelState.IsValid)
            {
                return View(exercise);
            }

            var exists = await _context.Exercises
                .AnyAsync(e =>
                    e.ExerciseName == exercise.ExerciseName);

            if (exists)
            {
                ModelState.AddModelError(
                    "ExerciseName",
                    "An exercise with this name already exists.");

                return View(exercise);
            }

            _context.Exercises.Add(exercise);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Exercise added successfully.";

            return RedirectToAction(nameof(Exercises));
        }

        public async Task<IActionResult> Equipment()
        {
            var equipment =
                await _context.Equipment
                    .OrderBy(e => e.EquipmentName)
                    .ToListAsync();

            return View(equipment);
        }

        [HttpGet]
        public IActionResult AddEquipment()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEquipment(
            Equipment equipment,
            IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(equipment);
            }

            equipment.IsAvailable = true;

            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

                var extension =
                    Path.GetExtension(imageFile.FileName)
                        .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "ImagePath",
                        "Only JPG, JPEG, PNG and WEBP images are allowed.");

                    return View(equipment);
                }

                var uploadFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "equipment");

                Directory.CreateDirectory(uploadFolder);

                var fileName =
                    $"{Guid.NewGuid()}{extension}";

                var filePath =
                    Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                equipment.ImagePath =
                    $"/uploads/equipment/{fileName}";
            }

            _context.Equipment.Add(equipment);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Equipment added successfully.";

            return RedirectToAction(nameof(Equipment));
        }

        public async Task<IActionResult> Workouts()
        {
            var workouts = await _context.WorkoutPlans
                .Include(w => w.Member)
                .Include(w => w.Exercise)
                .OrderByDescending(w => w.WorkoutPlanId)
                .ToListAsync();

            return View(workouts);
        }

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

        public async Task<IActionResult> ViewProfile(int id)
        {
            var student = await _context.Members
                .Include(m => m.WorkoutProfiles)
                .Include(m => m.WorkoutPlans)
                    .ThenInclude(w => w.Exercise)
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

        [HttpGet]
        public async Task<IActionResult> CreateWorkout()
        {
            var members = await _context.Members
                .Where(m =>
                    m.Role == "Student" ||
                    m.Role == "Staff")
                .ToListAsync();

            var exercises = await _context.Exercises
                .OrderBy(e => e.Category)
                .ThenBy(e => e.ExerciseName)
                .ToListAsync();

            ViewBag.Members = members;
            ViewBag.Exercises = exercises;

            return View();
        }

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

                ViewBag.Exercises = await _context.Exercises
                    .OrderBy(e => e.Category)
                    .ThenBy(e => e.ExerciseName)
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

                ViewBag.Exercises = await _context.Exercises
                    .OrderBy(e => e.Category)
                    .ThenBy(e => e.ExerciseName)
                    .ToListAsync();

                return View(workout);
            }

            if (workout.ExerciseId.HasValue)
            {
                var exerciseExists = await _context.Exercises
                    .AnyAsync(e =>
                        e.ExerciseId == workout.ExerciseId.Value);

                if (!exerciseExists)
                {
                    ModelState.AddModelError(
                        "ExerciseId",
                        "Please select a valid exercise.");

                    ViewBag.Members = await _context.Members
                        .Where(m =>
                            m.Role == "Student" ||
                            m.Role == "Staff")
                        .ToListAsync();

                    ViewBag.Exercises = await _context.Exercises
                        .OrderBy(e => e.Category)
                        .ThenBy(e => e.ExerciseName)
                        .ToListAsync();

                    return View(workout);
                }
            }

            _context.WorkoutPlans.Add(workout);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Workout plan assigned successfully.";

            return RedirectToAction(nameof(Workouts));
        }

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

            var activeRequest =
                await _context.TrainerRequests
                    .Include(r => r.Student)
                    .FirstOrDefaultAsync(r =>
                        r.TrainerId == trainer.TrainerId &&
                        r.Status == "Accepted");

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
                    "This request is no longer pending.";

                return RedirectToAction(nameof(Requests));
            }

            request.Status = "Accepted";
            request.ResponseDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Trainer request accepted. Complete this workout before accepting another student request.";

            return RedirectToAction(nameof(Requests));
        }

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