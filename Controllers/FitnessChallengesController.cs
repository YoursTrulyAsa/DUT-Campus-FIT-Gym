
using AutoMapper.Execution;
using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[Authorize(Roles = "Student,Staff")]
public class FitnessChallengesController : Controller
{
    private readonly GymDbContext _context;

    public FitnessChallengesController(GymDbContext context)
    {
        _context = context;
    }

    // Display challenge generation page
    public IActionResult Create()
    {
        return View();
    }

    // Generate challenge
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(FitnessChallengeViewModel model)
    {
        // 1. Validate user input
        // 2. Generate AI challenge
        // Validate user input
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var memberId = GetMemberId();

        if (memberId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Temporary fallback challenge
        var challenge = CreateFallbackChallenge(
            model.FitnessLevel,
            model.ExercisePreference
        );

        // Apply safety validation
        if (!IsSafeChallenge(
            challenge.Days.ToList(),
            model.FitnessLevel))
        {
            ModelState.AddModelError(
                "",
                "The generated challenge did not pass the safety requirements."
            );
            // Temporary return while we build AI generation
            return View(model);
        }
        challenge.MemberId = memberId.Value;

        // Save challenge
        _context.FitnessChallenges.Add(challenge);
        _context.SaveChanges();

        return RedirectToAction(
            nameof(MyChallenges)
        );
    }

    // View member's challenges
    public IActionResult MyChallenges()
    {
        var memberId = GetMemberId();

        if (memberId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var challenges = _context.FitnessChallenges
            .Include(c => c.Days)
            .Where(c => c.MemberId == memberId.Value)
            .OrderByDescending(c => c.CreatedDate)
            .ToList();
        // Get only logged-in member's challenges
        return View(challenges);
    }

    // Accept challenge
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AcceptChallenge(int id)
    {
        var memberId = GetMemberId();

        if (memberId == null)
            return RedirectToAction("Login", "Account");



        // Find the challenge being accepted
        var challenge = _context.FitnessChallenges
            .FirstOrDefault(c =>
                c.FitnessChallengeId == id &&
                c.MemberId == memberId.Value);

        if (challenge == null)
            return NotFound();

        if (challenge.Status != "Generated")
        {
            TempData["ChallengeError"] =
                "This challenge cannot be accepted.";

            return RedirectToAction(nameof(MyChallenges));
        }
        // Check if the user already has an active challenge
        bool alreadyHasActiveChallenge = _context.FitnessChallenges
            .Any(c =>
                c.MemberId == memberId.Value &&
                (c.Status == "Accepted" ||
                 c.Status == "InProgress"));

        if (alreadyHasActiveChallenge)
        {
            TempData["ChallengeError"] =
                "You already have an active fitness challenge. Complete or cancel it before accepting another challenge.";

            return RedirectToAction(nameof(MyChallenges));
        }

        // Accept the selected challenge
        challenge.Status = "Accepted";
        challenge.AcceptedDate = DateTime.Now;

        _context.SaveChanges();

        TempData["ChallengeSuccess"] =
            "Fitness challenge accepted successfully!";

        return RedirectToAction(nameof(MyChallenges));
    }

    // Update daily progress
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateProgress(int id, int completed)
    {
        var memberId = GetMemberId();

        if (memberId == null)
            return RedirectToAction("Login", "Account");

        var member = _context.Members
            .FirstOrDefault(m => m.MemberId == memberId.Value);

        if (member == null)
            return NotFound();

        // Find the challenge day and make sure it belongs
        // to the logged-in member
        var day = _context.FitnessChallengeDays
            .Include(d => d.FitnessChallenge)
            .ThenInclude(c => c.Days)
            .FirstOrDefault(d =>
                d.FitnessChallengeDayId == id &&
                d.FitnessChallenge!.MemberId == memberId.Value);

        if (day == null)
            return NotFound();

        var challenge = day.FitnessChallenge;

        // Only an active challenge can receive progress
        if (challenge.Status != "InProgress")
        {
            TempData["ChallengeError"] =
                "You can only update progress for a challenge that is in progress.";

            return RedirectToAction(nameof(MyChallenges));
        }

        // -----------------------------------------
        // ONE UPDATE PER DAY
        // -----------------------------------------
        System.Diagnostics.Debug.WriteLine(
                $"DAY ID: {day.FitnessChallengeDayId}, UPDATED: {day.UpdatedDate}"
               );
        if (day.UpdatedDate.HasValue &&
            day.UpdatedDate.Value.Date == DateTime.Now.Date)
        {
            TempData["ChallengeError"] =
                "You have already updated your progress for today.";

            return RedirectToAction(nameof(MyChallenges));
        }

        // -----------------------------------------
        // VALIDATE COMPLETED AMOUNT
        // -----------------------------------------

        if (completed < 0)
            completed = 0;

        if (completed > day.Target)
            completed = day.Target;

        // -----------------------------------------
        // SAVE TODAY'S PROGRESS
        // -----------------------------------------

        day.Completed = completed;

        day.IsCompleted = day.Completed >= day.Target;

        // Record when the update was made
        day.UpdatedDate = DateTime.Now;

        // -----------------------------------------
        // CHECK IF ALL DAYS ARE COMPLETED
        // -----------------------------------------

        var allDaysCompleted = _context.FitnessChallengeDays
            .Where(d => d.FitnessChallengeId == day.FitnessChallengeId)
            .All(d => d.IsCompleted);

        // -----------------------------------------
        // COMPLETE CHALLENGE + AWARD POINTS
        // -----------------------------------------

        if (allDaysCompleted && challenge.Status != "Completed")
        {
            challenge.Status = "Completed";

            // Award exactly 5 points
            challenge.PointsAwarded = 5;

            member.Points += 5;

            TempData["ChallengeSuccess"] =
                " 🎉 Congratulations! You completed the challenge and earned 5 points!";
        }
        else
        {
            TempData["ChallengeSuccess"] =
                "Today's progress was updated successfully.";
        }

        _context.SaveChanges();

        return RedirectToAction(nameof(MyChallenges));
    }

    // -------------------------
    // SAFETY RULES
    // -------------------------
    private const int MaxChallengeDays = 7;

    private int GetMaximumTarget(string fitnessLevel)
    {
        return fitnessLevel switch
        {
            "Beginner" => 50,
            "Intermediate" => 100,
            "Advanced" => 150,
            _ => 50
        };
    }

    private bool IsSafeChallenge(
    List<FitnessChallengeDay> days,
    string fitnessLevel)
    {
        // Rule 1: Maximum challenge duration
        if (days.Count > MaxChallengeDays)
            return false;

        int maximum = GetMaximumTarget(fitnessLevel);

        for (int i = 0; i < days.Count; i++)
        {
            // Rule 2: Target must be greater than zero
            if (days[i].Target <= 0)
                return false;

            // Rule 3: Target must not exceed maximum
            if (days[i].Target > maximum)
                return false;
            // Rule 4: Prevent extreme progression
            if (i > 0)
            {
                int previous = days[i - 1].Target;
                int current = days[i].Target;

                if (current > previous * 1.5)
                    return false;
            }
        }
        // Challenge passed all safety checks
        return true;
    }

    // =========================
    // FALLBACK CHALLENGE
    // =========================

    private FitnessChallenge CreateFallbackChallenge(
        string fitnessLevel,
        string exercise)
    {
        int[] targets = fitnessLevel switch
        {
            "Beginner" => new[] { 15, 20, 25, 30 },
            "Intermediate" => new[] { 30, 40, 50, 60 },
            "Advanced" => new[] { 50, 60, 70, 80 },
            _ => new[] { 15, 20, 25, 30 }
        };

        var challenge = new FitnessChallenge
        {
            Title = $"4-Day {exercise} Challenge",
            FitnessLevel = fitnessLevel,
            ExerciseType = exercise,
            DurationDays = 4,
            Status = "Generated"
        };

        for (int i = 0; i < targets.Length; i++)
        {
            challenge.Days.Add(new FitnessChallengeDay
            {
                DayNumber = i + 1,
                Exercise = exercise,
                Target = targets[i],
                Completed = 0,
                IsCompleted = false
            });
        }

        return challenge;
    }
    private int? GetMemberId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);

        if (claim == null)
            return null;

        if (int.TryParse(claim.Value, out int memberId))
            return memberId;

        return null;
    }
    //Start the Challenge
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult StartChallenge(int id)
    {
        var memberId = GetMemberId();

        if (memberId == null)
            return RedirectToAction("Login", "Account");

        var challenge = _context.FitnessChallenges
            .FirstOrDefault(c =>
                c.FitnessChallengeId == id &&
                c.MemberId == memberId.Value);

        if (challenge == null)
            return NotFound();

        // Only an accepted challenge can be started
        if (challenge.Status != "Accepted")
        {
            TempData["ChallengeError"] =
                "Only an accepted challenge can be started.";

            return RedirectToAction(nameof(MyChallenges));
        }

        challenge.Status = "InProgress";

        _context.SaveChanges();

        TempData["ChallengeSuccess"] =
            "Challenge started! Good luck.";

        return RedirectToAction(nameof(MyChallenges));
    }

    //Delete a challenge
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteChallenge(int id)
    {
        var memberId = GetMemberId();

        if (memberId == null)
            return RedirectToAction("Login", "Account");

        var challenge = _context.FitnessChallenges
            .FirstOrDefault(c =>
                c.FitnessChallengeId == id &&
                c.MemberId == memberId.Value);

        if (challenge == null)
            return NotFound();

        _context.FitnessChallenges.Remove(challenge);
        _context.SaveChanges();

        TempData["ChallengeSuccess"] =
            "Challenge deleted successfully.";

        return RedirectToAction(nameof(MyChallenges));
    }
}
