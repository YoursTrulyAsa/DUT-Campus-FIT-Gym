using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace DUT_Campus_FIT_Gym.Controllers
{
    public class AdminController : Controller
    {
        private readonly GymDbContext _context;
        private readonly PasswordHasher<Member> _passwordHasher;

        public AdminController(GymDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Member>();
        }

        // Admin Dashboard
        public IActionResult Index()
        {
            return View();
        }

        // ============================
        // ADD TRAINER
        // ============================

        [HttpGet]
        public IActionResult AddTrainer()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddTrainer(CreateStaffViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            bool emailExists = _context.Members
                .Any(m => m.Email == model.Email);

            bool numberExists = _context.Members
                .Any(m =>
                    m.StudentNumber ==
                    model.studentnumber);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered.");

                return View(model);
            }

            if (numberExists)
            {
                ModelState.AddModelError(
                    "StaffStudentNumber",
                    "This number is already registered.");

                return View(model);
            }

            var trainer = new Member
            {
                Name = model.FirstName,
                Surname = model.LastName,
                StudentNumber =
                    model.studentnumber,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,

                // Admin controls the role
                Role = "Trainer"
            };

            trainer.PasswordHash =
                _passwordHasher.HashPassword(
                    trainer,
                    model.Password);

            _context.Members.Add(trainer);
            _context.SaveChanges();

            TempData["Success"] =
                "Trainer account created successfully.";

            return RedirectToAction("Index");
        }

        public IActionResult Scanner()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyBarcodeCheckIn(string barcodeData)
        {
            if (string.IsNullOrWhiteSpace(barcodeData))
            {
                TempData["CheckInError"] =
                    "No barcode was detected.";

                return RedirectToAction("Scanner");
            }

            // Expected format:
            // DUTGYM:2

            if (!barcodeData.StartsWith("DUTGYM:"))
            {
                TempData["CheckInError"] =
                    "Invalid DUT FIT Gym barcode.";

                return RedirectToAction("Scanner");
            }

            var memberIdText =
                barcodeData.Substring("DUTGYM:".Length).Trim();

            if (!int.TryParse(memberIdText, out int memberId))
            {
                TempData["CheckInError"] =
                    "Invalid member identification.";

                return RedirectToAction("Scanner");
            }

            // Find member
            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId);

            if (member == null)
            {
                TempData["CheckInError"] =
                    "Member account could not be found.";

                return RedirectToAction("Scanner");
            }

            // Find latest membership
            var membership = _context.Memberships
                .Where(m => m.MemberId == memberId)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefault();

            if (membership == null)
            {
                TempData["CheckInError"] =
                    "This member does not have a gym membership.";

                return RedirectToAction("Scanner");
            }

            // Check membership expiry
            if (membership.EndDate.HasValue && membership.EndDate.Value.Date < DateTime.Today)
            {
                TempData["CheckInError"] =
                    "This member's gym membership has expired.";
                return RedirectToAction("Scanner");
            }

            // Check if already inside the gym
            var existingAttendance = _context.Attendances
                .FirstOrDefault(a =>
                    a.MemberId == memberId &&
                    a.CheckOutTime == null);

            if (existingAttendance != null)
            {
                TempData["CheckInError"] =
                    "This member is already checked in.";

                return RedirectToAction("Scanner");
            }

            // Create attendance
            var attendance = new Attendance
            {
                MemberId = memberId,
                CheckInTime = DateTime.Now,
                CheckOutTime = null
            };

            _context.Attendances.Add(attendance);

            _context.SaveChanges();

            TempData["CheckInSuccess"] =
                $"ACCESS GRANTED — Welcome {member.Name}!";

            return RedirectToAction("Scanner");
        }

    }
}
