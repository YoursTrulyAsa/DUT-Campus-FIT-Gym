using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Trainer")]
    public class TrainerController : Controller
    {
        private readonly GymDbContext _context;
        private readonly IConfiguration _config;

        public TrainerController(
            GymDbContext context,
            IConfiguration config)
        {
            _context = context;
            _config = config;
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

            TrainerBooking? currentBooking = null;

            if (activeRequest != null)
            {
                currentBooking = await _context.TrainerBookings
                    .FirstOrDefaultAsync(b =>
                        b.TrainerRequestId ==
                        activeRequest.TrainerRequestId &&
                        b.Status == "Booked");
            }

            ViewBag.ActiveRequest = activeRequest;
            ViewBag.CurrentBooking = currentBooking;
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
        public async Task<IActionResult> AddExercise(
            Exercise exercise)
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

        [HttpGet]
        public async Task<IActionResult> UpcomingSessions()
        {
            var trainerEmail =
                User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(trainerEmail))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var trainer =
                await _context.Trainers
                    .FirstOrDefaultAsync(t =>
                        t.Email == trainerEmail);

            if (trainer == null)
            {
                return NotFound(
                    "Trainer account was not found.");
            }

            var bookings =
                await _context.TrainerBookings
                    .Include(b => b.Student)
                    .Include(b => b.TrainerRequest)
                    .Where(b =>
                        b.TrainerId == trainer.TrainerId &&
                        b.Status == "Booked" &&
                        b.EndTime > DateTime.Now)
                    .OrderBy(b => b.StartTime)
                    .ToListAsync();

            return View(bookings);
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

            var currentMonth =
                DateTime.Now.Month;

            var currentYear =
                DateTime.Now.Year;

            var monthlyAssignments =
                await _context.TrainerRequests
                    .CountAsync(r =>
                        r.TrainerId == trainer.TrainerId &&
                        (r.Status == "Accepted" ||
                         r.Status == "Completed") &&
                        r.ResponseDate.HasValue &&
                        r.ResponseDate.Value.Month == currentMonth &&
                        r.ResponseDate.Value.Year == currentYear);

            var activeRequest =
                await _context.TrainerRequests
                    .Include(r => r.Student)
                    .FirstOrDefaultAsync(r =>
                        r.TrainerId == trainer.TrainerId &&
                        r.Status == "Accepted");

            TrainerBooking? currentBooking = null;

            if (activeRequest != null)
            {
                currentBooking =
                    await _context.TrainerBookings
                        .FirstOrDefaultAsync(b =>
                            b.TrainerRequestId ==
                            activeRequest.TrainerRequestId &&
                            b.Status == "Booked");
            }

            var requests =
                await _context.TrainerRequests
                    .Include(r => r.Student)
                        .ThenInclude(s => s.WorkoutProfiles)
                    .Include(r => r.Student)
                        .ThenInclude(s => s.WorkoutPlans)
                            .ThenInclude(w => w.Exercise)
                    .Where(r =>
                        r.TrainerId == trainer.TrainerId)
                    .OrderByDescending(r =>
                        r.RequestDate)
                    .ToListAsync();

            ViewBag.ActiveRequest =
                activeRequest;

            ViewBag.CurrentBooking =
                currentBooking;

            ViewBag.MonthlyAssignments =
                monthlyAssignments;

            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> ChallengeProofs()
        {
            var proofs =
                await _context.ChallengeProofs
                    .Include(p => p.Member)
                    .Include(p => p.FitnessChallenge)
                    .Where(p =>
                        p.Status == "PendingReview")
                    .OrderBy(p =>
                        p.SubmittedAt)
                    .AsNoTracking()
                    .ToListAsync();

            ViewBag.PendingProofs =
                proofs.Count;

            return View(proofs);
        }

        [HttpGet]
        public async Task<IActionResult> ReviewChallengeProof(
            int id)
        {
            var proof =
                await _context.ChallengeProofs
                    .Include(p => p.Member)
                    .Include(p => p.FitnessChallenge)
                        .ThenInclude(c => c.Participation)
                    .Include(p => p.ReviewedByTrainer)
                    .FirstOrDefaultAsync(p =>
                        p.ChallengeProofId == id);

            if (proof == null)
            {
                return NotFound(
                    "Challenge proof was not found.");
            }

            return View(proof);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveChallengeProof(
            int id,
            string? trainerComment)
        {
            var trainerEmail =
                User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(trainerEmail))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var trainer =
                await _context.Trainers
                    .FirstOrDefaultAsync(t =>
                        t.Email == trainerEmail);

            if (trainer == null)
            {
                return NotFound(
                    "Trainer account was not found.");
            }

            var proof =
                await _context.ChallengeProofs
                    .Include(p => p.FitnessChallenge)
                        .ThenInclude(c => c.Participation)
                    .FirstOrDefaultAsync(p =>
                        p.ChallengeProofId == id);

            if (proof == null)
            {
                TempData["Error"] =
                    "Challenge proof was not found.";

                return RedirectToAction(
                    nameof(ChallengeProofs));
            }

            if (proof.Status != "PendingReview")
            {
                TempData["Error"] =
                    "This proof has already been reviewed.";

                return RedirectToAction(
                    nameof(ChallengeProofs));
            }

            if (proof.FitnessChallenge == null ||
                proof.FitnessChallenge.Participation == null)
            {
                TempData["Error"] =
                    "The challenge participation record could not be found.";

                return RedirectToAction(
                    nameof(ChallengeProofs));
            }

            var challenge =
                proof.FitnessChallenge;

            var participation =
                challenge.Participation;

            var memberId =
                proof.MemberId;

            trainerComment =
                trainerComment?.Trim();

            if (!string.IsNullOrWhiteSpace(trainerComment) &&
                trainerComment.Length > 500)
            {
                TempData["Error"] =
                    "The trainer comment cannot exceed 500 characters.";

                return RedirectToAction(
                    nameof(ReviewChallengeProof),
                    new { id });
            }

            proof.Status =
                "Approved";

            proof.TrainerComment =
                string.IsNullOrWhiteSpace(trainerComment)
                    ? null
                    : trainerComment;

            proof.ReviewedAt =
                DateTime.Now;

            proof.ReviewedByTrainerId =
                trainer.TrainerId;

            challenge.Status =
                "Completed";

            participation.Status =
                "Completed";

            participation.CompletedAt =
                DateTime.Now;

            var rewardPoints =
                challenge.RewardPoints;

            var rewardReason =
                $"Completed fitness challenge: {challenge.FitnessChallengeId}";

            var alreadyRewarded =
                await _context.RewardPoints
                    .AnyAsync(r =>
                        r.MemberId == memberId &&
                        r.Reason == rewardReason);

            if (!alreadyRewarded)
            {
                _context.RewardPoints.Add(
                    new RewardPoint
                    {
                        MemberId =
                            memberId,

                        Points =
                            rewardPoints,

                        Reason =
                            rewardReason,

                        EarnedAt =
                            DateTime.Now
                    });
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Challenge proof approved. {rewardPoints} reward points have been awarded.";

            return RedirectToAction(
                nameof(ChallengeProofs));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectChallengeProof(
            int id,
            string? trainerComment)
        {
            var trainerEmail =
                User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(trainerEmail))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var trainer =
                await _context.Trainers
                    .FirstOrDefaultAsync(t =>
                        t.Email == trainerEmail);

            if (trainer == null)
            {
                return NotFound(
                    "Trainer account was not found.");
            }

            var proof =
                await _context.ChallengeProofs
                    .Include(p => p.FitnessChallenge)
                        .ThenInclude(c => c.Participation)
                    .FirstOrDefaultAsync(p =>
                        p.ChallengeProofId == id);

            if (proof == null)
            {
                TempData["Error"] =
                    "Challenge proof was not found.";

                return RedirectToAction(
                    nameof(ChallengeProofs));
            }

            if (proof.Status != "PendingReview")
            {
                TempData["Error"] =
                    "This proof has already been reviewed.";

                return RedirectToAction(
                    nameof(ChallengeProofs));
            }

            trainerComment =
                trainerComment?.Trim();

            if (string.IsNullOrWhiteSpace(trainerComment))
            {
                TempData["Error"] =
                    "Please provide a reason when rejecting a proof.";

                return RedirectToAction(
                    nameof(ReviewChallengeProof),
                    new { id });
            }

            if (trainerComment.Length > 500)
            {
                TempData["Error"] =
                    "The trainer comment cannot exceed 500 characters.";

                return RedirectToAction(
                    nameof(ReviewChallengeProof),
                    new { id });
            }

            proof.Status =
                "Rejected";

            proof.TrainerComment =
                trainerComment;

            proof.ReviewedAt =
                DateTime.Now;

            proof.ReviewedByTrainerId =
                trainer.TrainerId;

            if (proof.FitnessChallenge != null)
            {
                proof.FitnessChallenge.Status =
                    "Rejected";
            }

            if (proof.FitnessChallenge?.Participation != null)
            {
                proof.FitnessChallenge.Participation.Status =
                    "Rejected";
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Challenge proof rejected. The member can submit new proof.";

            return RedirectToAction(
                nameof(ChallengeProofs));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var trainerEmail =
                User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(trainerEmail))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var trainer =
                await _context.Trainers
                    .FirstOrDefaultAsync(t =>
                        t.Email == trainerEmail);

            if (trainer == null)
            {
                return NotFound(
                    "Trainer account was not found.");
            }

            var request =
                await _context.TrainerRequests
                    .Include(r => r.Student)
                    .FirstOrDefaultAsync(r =>
                        r.TrainerRequestId == id &&
                        r.TrainerId == trainer.TrainerId);

            if (request == null)
            {
                TempData["Error"] =
                    "Trainer request was not found.";

                return RedirectToAction(
                    nameof(Requests));
            }

            if (request.Status != "Pending")
            {
                TempData["Error"] =
                    "This request is no longer pending.";

                return RedirectToAction(
                    nameof(Requests));
            }

            var studentAlreadyAssigned =
                await _context.TrainerRequests
                    .AnyAsync(r =>
                        r.StudentId == request.StudentId &&
                        r.Status == "Accepted");

            if (studentAlreadyAssigned)
            {
                TempData["Error"] =
                    "This student is already assigned to a trainer.";

                return RedirectToAction(
                    nameof(Requests));
            }

            var currentMonth =
                DateTime.Now.Month;

            var currentYear =
                DateTime.Now.Year;

            var monthlyAssignments =
                await _context.TrainerRequests
                    .CountAsync(r =>
                        r.TrainerId == trainer.TrainerId &&
                        (r.Status == "Accepted" ||
                         r.Status == "Completed") &&
                        r.ResponseDate.HasValue &&
                        r.ResponseDate.Value.Month == currentMonth &&
                        r.ResponseDate.Value.Year == currentYear);

            if (monthlyAssignments >= 2)
            {
                TempData["Error"] =
                    "You have reached the maximum of 2 active student assignments for this month.";

                return RedirectToAction(
                    nameof(Requests));
            }

            request.Status =
                "Accepted";

            request.ResponseDate =
                DateTime.Now;

            await _context.SaveChangesAsync();

            if (request.Student != null)
            {
                try
                {
                    await SendTrainerAcceptedEmail(
                        request.Student,
                        trainer);
                }
                catch
                {
                }
            }

            TempData["Success"] =
                $"Trainer request accepted. {request.Student?.Name ?? "The student"} has been notified by email and can now book a training session.";

            return RedirectToAction(
                nameof(Requests));
        }

        private async Task SendTrainerAcceptedEmail(
            Member member,
            Trainer trainer)
        {
            var smtpSettings =
                _config.GetSection("SmtpSettings");

            string server =
                smtpSettings["Server"] ?? "";

            string portValue =
                smtpSettings["Port"] ?? "587";

            string senderEmail =
                smtpSettings["SenderEmail"] ?? "";

            string password =
                smtpSettings["Password"] ?? "";

            if (string.IsNullOrWhiteSpace(server) ||
                string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(member.Email))
            {
                return;
            }

            if (!int.TryParse(
                portValue,
                out int port))
            {
                port = 587;
            }

            var message =
                new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    "DUT Campus FIT Gym",
                    senderEmail));

            message.To.Add(
                new MailboxAddress(
                    $"{member.Name} {member.Surname}",
                    member.Email));

            message.Subject =
                "Trainer Request Accepted - DUT Campus FIT Gym";

            var builder =
                new BodyBuilder();

            builder.TextBody =
                $"Hello {member.Name},\n\n" +
                $"Your trainer request has been accepted by {trainer.TrainerName}.\n\n" +
                "You can now log into DUT Campus FIT Gym and book a training session with your trainer.\n\n" +
                "You can choose a future date and time that works for you and your trainer. " +
                "Training sessions are limited to one hour.\n\n" +
                $"Trainer: {trainer.TrainerName}\n" +
                $"Training Category: {trainer.Category ?? "General Fitness"}\n\n" +
                "Please log into DUT Campus FIT Gym to schedule your session.\n\n" +
                "Regards,\n" +
                "DUT Campus FIT Gym";

            message.Body =
                builder.ToMessageBody();

            using var client =
                new SmtpClient();

            client.ServerCertificateValidationCallback =
                (s, c, h, e) => true;

            await client.ConnectAsync(
                server,
                port,
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                senderEmail,
                password);

            await client.SendAsync(message);

            await client.DisconnectAsync(true);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectRequest(
            int id,
            string rejectionReason)
        {
            var trainerEmail =
                User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(trainerEmail))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var trainer =
                await _context.Trainers
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
                return NotFound(
                    "Trainer request was not found.");
            }

            if (request.Status != "Pending")
            {
                TempData["Error"] =
                    "Only pending requests can be rejected.";

                return RedirectToAction(
                    nameof(Requests));
            }

            if (string.IsNullOrWhiteSpace(
                rejectionReason))
            {
                TempData["Error"] =
                    "Please provide a reason for rejecting the request.";

                return RedirectToAction(
                    nameof(Requests));
            }

            rejectionReason =
                rejectionReason.Trim();

            if (rejectionReason.Length > 500)
            {
                TempData["Error"] =
                    "The rejection reason cannot exceed 500 characters.";

                return RedirectToAction(
                    nameof(Requests));
            }

            request.Status =
                "Rejected";

            request.RejectionReason =
                rejectionReason;

            request.ResponseDate =
                DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "The trainer request has been rejected.";

            return RedirectToAction(
                nameof(Requests));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteRequest(int id)
        {
            var trainerEmail =
                User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(trainerEmail))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t =>
                    t.Email == trainerEmail);

            if (trainer == null)
            {
                return NotFound(
                    "Trainer account was not found.");
            }

            var booking = await _context.TrainerBookings
                .Include(b => b.TrainerRequest)
                .FirstOrDefaultAsync(b =>
                    b.TrainerId == trainer.TrainerId &&
                    b.TrainerRequestId == id &&
                    b.Status == "Booked");

            if (booking == null)
            {
                TempData["Error"] =
                    "There is no active booked training session to complete.";

                return RedirectToAction(
                    nameof(Requests));
            }

            if (DateTime.Now < booking.StartTime)
            {
                TempData["Error"] =
                    "This training session has not started yet.";

                return RedirectToAction(
                    nameof(Requests));
            }

            booking.Status =
                "Completed";

            booking.CompletedDate =
                DateTime.Now;

            if (booking.TrainerRequest != null)
            {
                booking.TrainerRequest.Status =
                    "Completed";

                booking.TrainerRequest.CompletionDate =
                    DateTime.Now;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Training session completed successfully.";

            return RedirectToAction(
                nameof(Requests));
        }

        [HttpGet]
        public async Task<IActionResult> PrivateTrainer()
        {
            var trainerEmail =
                User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(trainerEmail))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var trainer =
                await _context.Trainers
                    .FirstOrDefaultAsync(t =>
                        t.Email == trainerEmail);

            if (trainer == null)
            {
                return NotFound(
                    "Trainer account was not found.");
            }

            var privateMembers =
                await _context.PrivateTrainerSubscriptions
                    .Include(s => s.Member)
                    .Where(s =>
                        s.TrainerId == trainer.TrainerId &&
                        s.Status == "Active" &&
                        s.EndDate >= DateTime.Now)
                    .OrderBy(s => s.EndDate)
                    .ToListAsync();

            ViewBag.Trainer =
                trainer;

            ViewBag.PrivateMemberCount =
                privateMembers.Count;

            ViewBag.PrivateTrainerLimit =
                2;

            return View(privateMembers);
        }

    }
}