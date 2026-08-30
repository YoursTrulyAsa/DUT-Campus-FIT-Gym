using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DUT_Campus_FIT_Gym.Controllers
{
    public class WorkoutProfileController : Controller
    {
        private readonly GymDbContext _context;

        public WorkoutProfileController(GymDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            int id = int.Parse(memberId);

            var profile = _context.WorkoutProfiles
                .FirstOrDefault(p => p.MemberId == id);

            return View(profile);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(WorkoutProfile profile)
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            profile.MemberId = int.Parse(memberId);

            var existingProfile = _context.WorkoutProfiles
                .FirstOrDefault(p => p.MemberId == profile.MemberId);

            if (existingProfile != null)
            {
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                _context.WorkoutProfiles.Add(profile);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(profile);
        }

        [HttpGet]
        public IActionResult Edit()
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            int id = int.Parse(memberId);

            var profile = _context.WorkoutProfiles
                .FirstOrDefault(p => p.MemberId == id);

            if (profile == null)
            {
                return RedirectToAction("Create");
            }

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(WorkoutProfile profile)
        {
            var memberId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            int currentMemberId = int.Parse(memberId);

            var existingProfile = _context.WorkoutProfiles
                .FirstOrDefault(p => p.WorkoutProfileId == profile.WorkoutProfileId &&
                                     p.MemberId == currentMemberId);

            if (existingProfile == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                existingProfile.Age = profile.Age;
                existingProfile.Weight = profile.Weight;
                existingProfile.Height = profile.Height;
                existingProfile.Goal = profile.Goal;

                _context.SaveChanges();

                return RedirectToAction("Index");
            }

            return View(profile);
        }
    }
}
