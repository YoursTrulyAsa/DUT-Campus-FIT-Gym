using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Student,Staff")]
    public class WorkoutController : Controller
    {
        private readonly GymDbContext _context;
        private readonly RewardService _rewardService;

        private readonly IWebHostEnvironment _environment;

        public WorkoutController(
            GymDbContext context,
            RewardService rewardService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _rewardService = rewardService;
            _environment = environment;
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
                        var completedProgrammeId =
                            TempData["CompletedProgrammeId"] as int?;

                        WorkoutProgramme? completedProgramme = null;

                        if (completedProgrammeId.HasValue)
                        {
                            completedProgramme =
                                await _context.WorkoutProgrammes
                                    .Where(p =>
                                        p.WorkoutProgrammeId ==
                                            completedProgrammeId.Value &&
                                        p.MemberId == memberId &&
                                        p.IsCompleted)
                                    .FirstOrDefaultAsync();
                        }

                        if (completedProgramme == null)
                        {
                            completedProgramme =
                                await _context.WorkoutProgrammes
                                    .Where(p =>
                                        p.MemberId == memberId &&
                                        p.IsCompleted)
                                    .OrderByDescending(p => p.CreatedAt)
                                    .FirstOrDefaultAsync();
                        }

                        if (completedProgramme != null)
                        {
                            ViewBag.CompletedProgramme =
                                completedProgramme;
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
        public async Task<IActionResult> FavouriteProgrammes()
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(
                memberIdClaim,
                out int memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var favouriteProgrammes =
                await _context.WorkoutProgrammes
                    .Where(p =>
                        p.MemberId == memberId &&
                        p.IsFavourite &&
                        p.IsCompleted)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

            return View(favouriteProgrammes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetakeWorkout(int workoutProgrammeId)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var originalProgramme =
                await _context.WorkoutProgrammes
                    .FirstOrDefaultAsync(p =>
                        p.WorkoutProgrammeId == workoutProgrammeId &&
                        p.MemberId == memberId &&
                        p.IsFavourite &&
                        p.IsCompleted);

            if (originalProgramme == null)
            {
                TempData["WorkoutFavouriteMessage"] =
                    "That workout programme is not available in your favourites.";

                return RedirectToAction(nameof(FavouriteProgrammes));
            }

            var originalWorkouts =
                await _context.WorkoutPlans
                    .Where(w =>
                        w.MemberId == memberId &&
                        w.WorkoutProgrammeId ==
                            originalProgramme.WorkoutProgrammeId)
                    .OrderBy(w => w.WeekNumber)
                    .ThenBy(w => w.WorkoutDay)
                    .ThenBy(w => w.WorkoutPlanId)
                    .ToListAsync();

            if (!originalWorkouts.Any())
            {
                TempData["WorkoutFavouriteMessage"] =
                    "This workout programme has no workout days to retake.";

                return RedirectToAction(nameof(FavouriteProgrammes));
            }

            var activeProgramme =
                await _context.WorkoutProgrammes
                    .FirstOrDefaultAsync(p =>
                        p.MemberId == memberId &&
                        !p.IsCompleted);

            if (activeProgramme != null)
            {
                TempData["WorkoutFavouriteMessage"] =
                    "You already have an active workout programme. Complete it before starting a retake.";

                return RedirectToAction(nameof(FavouriteProgrammes));
            }

            var now = DateTime.Now;

            var retakeProgramme =
                new WorkoutProgramme
                {
                    MemberId = memberId,
                    ProgrammeName = originalProgramme.ProgrammeName,
                    FitnessLevel = originalProgramme.FitnessLevel,
                    Goal = originalProgramme.Goal,
                    StartDate = now,
                    EndDate = now.AddDays(27),
                    IsCompleted = false,
                    IsFavourite = false,
                    CreatedAt = now
                };

            _context.WorkoutProgrammes.Add(retakeProgramme);

            await _context.SaveChangesAsync();

            var retakeWorkouts =
                originalWorkouts
                    .Select(w =>
                        new WorkoutPlan
                        {
                            MemberId = memberId,
                            WorkoutProgrammeId =
                                retakeProgramme.WorkoutProgrammeId,
                            WorkoutName = w.WorkoutName,
                            ExerciseName = w.ExerciseName,
                            ExerciseId = w.ExerciseId,
                            Level = w.Level,
                            WorkoutDay = w.WorkoutDay,
                            WeekNumber = w.WeekNumber,
                            Sets = w.Sets,
                            Repetitions = w.Repetitions,
                            RestTime = w.RestTime,
                            Description = w.Description
                        })
                    .ToList();

            _context.WorkoutPlans.AddRange(retakeWorkouts);

            await _context.SaveChangesAsync();

            TempData["WorkoutFavouriteMessage"] =
                $"Retaking {retakeProgramme.ProgrammeName}. Your previous result has been kept.";

            return RedirectToAction(nameof(MyWorkout));
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFavourite(
    int workoutProgrammeId)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(
                memberIdClaim,
                out int memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var programme =
                await _context.WorkoutProgrammes
                    .FirstOrDefaultAsync(p =>
                        p.WorkoutProgrammeId ==
                            workoutProgrammeId &&
                        p.MemberId == memberId);

            if (programme == null)
            {
                return NotFound();
            }

            if (!programme.IsCompleted)
            {
                TempData["WorkoutFavouriteMessage"] =
                    "You can only save a workout programme after completing it.";

                return RedirectToAction(
                    nameof(MyWorkout));
            }

            programme.IsFavourite =
                !programme.IsFavourite;

            await _context.SaveChangesAsync();

            TempData["WorkoutFavouriteMessage"] =
                programme.IsFavourite
                    ? "Workout programme saved to your favourites."
                    : "Workout programme removed from your favourites.";

            if (programme.IsCompleted)
            {
                TempData["ProgrammeCompleted"] = "true";
                TempData["CompletedProgrammeId"] =
                    programme.WorkoutProgrammeId;
                TempData["CompletedFitnessLevel"] =
                    programme.FitnessLevel;
                TempData["CompletedProgrammeName"] =
                    programme.ProgrammeName;
            }

            return RedirectToAction(
                nameof(MyWorkout));
        }

        [HttpGet]
        public async Task<IActionResult> ShareResult(int workoutProgrammeId)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var programme =
                await _context.WorkoutProgrammes
                    .FirstOrDefaultAsync(p =>
                        p.WorkoutProgrammeId == workoutProgrammeId &&
                        p.MemberId == memberId &&
                        p.IsCompleted);

            if (programme == null)
                return NotFound();

            var existingResult =
                await _context.WorkoutResults
                    .FirstOrDefaultAsync(r =>
                        r.WorkoutProgrammeId == workoutProgrammeId &&
                        r.MemberId == memberId);

            if (existingResult != null)
            {
                return RedirectToAction(
                    nameof(MyResults));
            }

            ViewBag.WorkoutProgramme = programme;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShareResult(
            int workoutProgrammeId,
            IFormFile beforePhoto,
            IFormFile afterPhoto,
            string description)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var programme =
                await _context.WorkoutProgrammes
                    .FirstOrDefaultAsync(p =>
                        p.WorkoutProgrammeId == workoutProgrammeId &&
                        p.MemberId == memberId &&
                        p.IsCompleted);

            if (programme == null)
                return NotFound();

            var existingResult =
                await _context.WorkoutResults
                    .AnyAsync(r =>
                        r.WorkoutProgrammeId == workoutProgrammeId &&
                        r.MemberId == memberId);

            if (existingResult)
            {
                TempData["WorkoutResultMessage"] =
                    "You have already shared this workout programme.";

                return RedirectToAction(nameof(MyResults));
            }

            if (beforePhoto == null ||
                beforePhoto.Length == 0)
            {
                ModelState.AddModelError(
                    "beforePhoto",
                    "Please upload a before photo.");
            }

            if (afterPhoto == null ||
                afterPhoto.Length == 0)
            {
                ModelState.AddModelError(
                    "afterPhoto",
                    "Please upload an after photo.");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                ModelState.AddModelError(
                    "description",
                    "Please add a description.");
            }

            if (description?.Length > 1000)
            {
                ModelState.AddModelError(
                    "description",
                    "The description cannot exceed 1000 characters.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.WorkoutProgramme = programme;
                return View();
            }

            var allowedExtensions =
                new[] { ".jpg", ".jpeg", ".png", ".webp" };

            var beforeExtension =
                Path.GetExtension(beforePhoto.FileName)
                    .ToLowerInvariant();

            var afterExtension =
                Path.GetExtension(afterPhoto.FileName)
                    .ToLowerInvariant();

            if (!allowedExtensions.Contains(beforeExtension))
            {
                ModelState.AddModelError(
                    "beforePhoto",
                    "Only JPG, JPEG, PNG and WEBP images are allowed.");
            }

            if (!allowedExtensions.Contains(afterExtension))
            {
                ModelState.AddModelError(
                    "afterPhoto",
                    "Only JPG, JPEG, PNG and WEBP images are allowed.");
            }

            const long maxFileSize = 5 * 1024 * 1024;

            if (beforePhoto.Length > maxFileSize)
            {
                ModelState.AddModelError(
                    "beforePhoto",
                    "The before photo cannot exceed 5 MB.");
            }

            if (afterPhoto.Length > maxFileSize)
            {
                ModelState.AddModelError(
                    "afterPhoto",
                    "The after photo cannot exceed 5 MB.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.WorkoutProgramme = programme;
                return View();
            }

            var uploadFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "workout-results");

            Directory.CreateDirectory(uploadFolder);

            var beforeFileName =
                $"{Guid.NewGuid():N}{beforeExtension}";

            var afterFileName =
                $"{Guid.NewGuid():N}{afterExtension}";

            var beforePath =
                Path.Combine(
                    uploadFolder,
                    beforeFileName);

            var afterPath =
                Path.Combine(
                    uploadFolder,
                    afterFileName);

            await using (var beforeStream =
                new FileStream(
                    beforePath,
                    FileMode.Create))
            {
                await beforePhoto.CopyToAsync(beforeStream);
            }

            await using (var afterStream =
                new FileStream(
                    afterPath,
                    FileMode.Create))
            {
                await afterPhoto.CopyToAsync(afterStream);
            }

            var result =
                new WorkoutResult
                {
                    MemberId = memberId,
                    WorkoutProgrammeId =
                        workoutProgrammeId,
                    BeforePhoto =
                        $"/uploads/workout-results/{beforeFileName}",
                    AfterPhoto =
                        $"/uploads/workout-results/{afterFileName}",
                    Description =
                        description.Trim(),
                    SharedAt =
                        DateTime.Now
                };

            _context.WorkoutResults.Add(result);

            await _context.SaveChangesAsync();

            TempData["WorkoutResultMessage"] =
                "Your workout result has been shared successfully.";

            return RedirectToAction(nameof(MyResults));
        }

        [HttpGet]
        public async Task<IActionResult> CommunityResults()
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var results =
                await _context.WorkoutResults
                    .Include(r => r.Member)
                    .Include(r => r.WorkoutProgramme)
                    .OrderByDescending(r => r.SharedAt)
                    .ToListAsync();

            var savedResultIds =
                await _context.SavedWorkoutResults
                    .Where(s => s.MemberId == memberId)
                    .Select(s => s.WorkoutResultId)
                    .ToListAsync();

            ViewBag.SavedResultIds =
                savedResultIds.ToHashSet();

            ViewBag.CurrentMemberId =
                memberId;

            return View(results);
        }

        [HttpGet]
        public async Task<IActionResult> MyResults()
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var results =
                await _context.WorkoutResults
                    .Include(r => r.WorkoutProgramme)
                    .Where(r => r.MemberId == memberId)
                    .OrderByDescending(r => r.SharedAt)
                    .ToListAsync();

            return View(results);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSaveResult(
            int workoutResultId)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var result =
                await _context.WorkoutResults
                    .FirstOrDefaultAsync(r =>
                        r.WorkoutResultId ==
                            workoutResultId);

            if (result == null)
                return NotFound();

            var saved =
                await _context.SavedWorkoutResults
                    .FirstOrDefaultAsync(s =>
                        s.MemberId == memberId &&
                        s.WorkoutResultId ==
                            workoutResultId);

            if (saved == null)
            {
                _context.SavedWorkoutResults.Add(
                    new SavedWorkoutResult
                    {
                        MemberId = memberId,
                        WorkoutResultId =
                            workoutResultId,
                        SavedAt = DateTime.Now
                    });

                TempData["WorkoutResultMessage"] =
                    "Workout result saved.";
            }
            else
            {
                _context.SavedWorkoutResults.Remove(saved);

                TempData["WorkoutResultMessage"] =
                    "Workout result removed from your saved results.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(CommunityResults));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DoThisWorkout(int workoutResultId)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var result =
                await _context.WorkoutResults
                    .Include(r => r.WorkoutProgramme)
                    .FirstOrDefaultAsync(r =>
                        r.WorkoutResultId == workoutResultId);

            if (result == null ||
                result.WorkoutProgramme == null)
            {
                return NotFound();
            }

            if (result.MemberId == memberId)
            {
                TempData["WorkoutResultMessage"] =
                    "This is your own shared workout result. You can retake it from Favourite Programmes.";

                return RedirectToAction(nameof(CommunityResults));
            }

            var originalProgramme = result.WorkoutProgramme;

            var activeProgramme =
                await _context.WorkoutProgrammes
                    .FirstOrDefaultAsync(p =>
                        p.MemberId == memberId &&
                        !p.IsCompleted);

            if (activeProgramme != null)
            {
                TempData["WorkoutResultMessage"] =
                    "You already have an active workout programme. Complete it before starting another workout.";

                return RedirectToAction(nameof(CommunityResults));
            }

            var originalWorkouts =
                await _context.WorkoutPlans
                    .Where(w =>
                        w.MemberId == originalProgramme.MemberId &&
                        w.WorkoutProgrammeId ==
                            originalProgramme.WorkoutProgrammeId)
                    .OrderBy(w => w.WeekNumber)
                    .ThenBy(w => w.WorkoutDay)
                    .ThenBy(w => w.WorkoutPlanId)
                    .ToListAsync();

            if (!originalWorkouts.Any())
            {
                TempData["WorkoutResultMessage"] =
                    "This shared workout programme is no longer available.";

                return RedirectToAction(nameof(CommunityResults));
            }

            var now = DateTime.Now;

            var newProgramme =
                new WorkoutProgramme
                {
                    MemberId = memberId,
                    ProgrammeName = originalProgramme.ProgrammeName,
                    FitnessLevel = originalProgramme.FitnessLevel,
                    Goal = originalProgramme.Goal,
                    StartDate = now,
                    EndDate = now.AddDays(27),
                    IsCompleted = false,
                    IsFavourite = false,
                    CreatedAt = now
                };

            _context.WorkoutProgrammes.Add(newProgramme);

            await _context.SaveChangesAsync();

            var newWorkouts =
                originalWorkouts
                    .Select(w =>
                        new WorkoutPlan
                        {
                            MemberId = memberId,
                            WorkoutProgrammeId =
                                newProgramme.WorkoutProgrammeId,
                            WorkoutName = w.WorkoutName,
                            ExerciseName = w.ExerciseName,
                            ExerciseId = w.ExerciseId,
                            Level = w.Level,
                            WorkoutDay = w.WorkoutDay,
                            WeekNumber = w.WeekNumber,
                            Sets = w.Sets,
                            Repetitions = w.Repetitions,
                            RestTime = w.RestTime,
                            Description = w.Description
                        })
                    .ToList();

            _context.WorkoutPlans.AddRange(newWorkouts);

            await _context.SaveChangesAsync();

            TempData["WorkoutResultMessage"] =
                $"You started {newProgramme.ProgrammeName}. You can now work through the programme and share your own result.";

            return RedirectToAction(nameof(MyWorkout));
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