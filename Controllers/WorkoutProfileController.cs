using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Student,Staff")]
    public class WorkoutProfileController : Controller
    {
        private readonly GymDbContext _context;

        public WorkoutProfileController(GymDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var profile = await _context.WorkoutProfiles
                .FirstOrDefaultAsync(p => p.MemberId == memberId);

            var weightHistory = await _context.WeightHistories
                .Where(w => w.MemberId == memberId)
                .OrderByDescending(w => w.RecordedAt)
                .ToListAsync();

            ViewBag.WeightHistory = weightHistory;

            return View(profile);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var existingProfile = await _context.WorkoutProfiles
                .FirstOrDefaultAsync(p => p.MemberId == memberId);

            if (existingProfile != null)
                return RedirectToAction(nameof(Index));

            return View(new WorkoutProfile
            {
                FitnessLevel = "Beginner",
                Goal = "General fitness"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkoutProfile profile)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var existingProfile = await _context.WorkoutProfiles
                .FirstOrDefaultAsync(p => p.MemberId == memberId);

            if (existingProfile != null)
                return RedirectToAction(nameof(Index));

            profile.MemberId = memberId;

            ValidateProfileSelections(profile);

            if (!ModelState.IsValid)
                return View(profile);

            _context.WorkoutProfiles.Add(profile);

            _context.WeightHistories.Add(
                new WeightHistory
                {
                    MemberId = memberId,
                    Weight = profile.Weight,
                    RecordedAt = DateTime.Now
                });

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var profile = await _context.WorkoutProfiles
                .FirstOrDefaultAsync(p => p.MemberId == memberId);

            if (profile == null)
                return RedirectToAction(nameof(Create));

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(WorkoutProfile profile)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int currentMemberId))
                return RedirectToAction("Login", "Account");

            var existingProfile = await _context.WorkoutProfiles
                .FirstOrDefaultAsync(p =>
                    p.WorkoutProfileId == profile.WorkoutProfileId &&
                    p.MemberId == currentMemberId);

            if (existingProfile == null)
                return NotFound();

            ValidateProfileSelections(profile);

            if (!ModelState.IsValid)
                return View(profile);

            var weightChanged =
                Math.Abs(existingProfile.Weight - profile.Weight) > 0.001;

            existingProfile.Age = profile.Age;
            existingProfile.Weight = profile.Weight;
            existingProfile.Height = profile.Height;
            existingProfile.FitnessLevel = profile.FitnessLevel;
            existingProfile.Goal = profile.Goal;

            if (weightChanged)
            {
                _context.WeightHistories.Add(
                    new WeightHistory
                    {
                        MemberId = currentMemberId,
                        Weight = profile.Weight,
                        RecordedAt = DateTime.Now
                    });
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private void ValidateProfileSelections(WorkoutProfile profile)
        {
            var validLevels = new[]
            {
                "Beginner",
                "Intermediate",
                "Pro"
            };

            var validGoals = new[]
            {
                "General fitness",
                "Strength",
                "Endurance",
                "Mobility and flexibility"
            };

            if (!validLevels.Contains(
                    profile.FitnessLevel,
                    StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(profile.FitnessLevel),
                    "Please select a valid fitness level.");
            }

            if (!validGoals.Contains(
                    profile.Goal,
                    StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    nameof(profile.Goal),
                    "Please select a valid fitness goal.");
            }
        }
    }
}