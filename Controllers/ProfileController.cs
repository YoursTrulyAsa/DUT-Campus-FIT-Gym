using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Student,Staff")]
    public class ProfileController : Controller
    {
        private readonly GymDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(
            GymDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var memberIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == memberId);

            if (member == null)
                return NotFound();

            return View(member);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveChanges(
            string name,
            string surname,
            string phoneNumber)
        {
            var memberIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == memberId);

            if (member == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(name) ||
                string.IsNullOrWhiteSpace(surname) ||
                string.IsNullOrWhiteSpace(phoneNumber))
            {
                TempData["ProfileError"] =
                    "Please complete all required profile fields.";

                return RedirectToAction(nameof(Index));
            }

            member.Name = name.Trim();
            member.Surname = surname.Trim();
            member.PhoneNumber = phoneNumber.Trim();

            await _context.SaveChangesAsync();

            TempData["ProfileSuccess"] =
                "Your profile details have been updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPhoto(IFormFile profilePicture)
        {
            var memberIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == memberId);

            if (member == null)
                return NotFound();

            if (profilePicture == null || profilePicture.Length == 0)
            {
                TempData["ProfileError"] =
                    "Please select a profile picture.";

                return RedirectToAction(nameof(Index));
            }

            if (profilePicture.Length > 5 * 1024 * 1024)
            {
                TempData["ProfileError"] =
                    "The profile picture must be 5 MB or smaller.";

                return RedirectToAction(nameof(Index));
            }

            var extension =
                Path.GetExtension(profilePicture.FileName)
                    .ToLowerInvariant();

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            if (!allowedExtensions.Contains(extension))
            {
                TempData["ProfileError"] =
                    "Only JPG, JPEG, PNG and WebP images are allowed.";

                return RedirectToAction(nameof(Index));
            }

            var uploadFolder = Path.Combine(
                _environment.WebRootPath,
                "uploads",
                "profile");

            Directory.CreateDirectory(uploadFolder);

            var fileName =
                $"{member.MemberId}_{Guid.NewGuid():N}{extension}";

            var filePath = Path.Combine(
                uploadFolder,
                fileName);

            await using (var stream = new FileStream(
                filePath,
                FileMode.Create))
            {
                await profilePicture.CopyToAsync(stream);
            }

            var oldPicture = member.ProfilePicture;

            member.ProfilePicture =
                $"/uploads/profile/{fileName}";

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(oldPicture))
            {
                var oldFileName = Path.GetFileName(oldPicture);

                var oldFilePath = Path.Combine(
                    uploadFolder,
                    oldFileName);

                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            TempData["ProfileSuccess"] =
                "Your profile picture has been updated successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemovePhoto()
        {
            var memberIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
                return RedirectToAction("Login", "Account");

            var member = await _context.Members
                .FirstOrDefaultAsync(m => m.MemberId == memberId);

            if (member == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(member.ProfilePicture))
            {
                var fileName =
                    Path.GetFileName(member.ProfilePicture);

                var filePath = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "profile",
                    fileName);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                member.ProfilePicture = null;

                await _context.SaveChangesAsync();
            }

            TempData["ProfileSuccess"] =
                "Your profile picture has been removed.";

            return RedirectToAction(nameof(Index));
        }
    }
}