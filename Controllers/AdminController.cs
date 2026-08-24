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
                    m.StaffStudentNumber ==
                    model.StaffStudentNumber);

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
                FirstName = model.FirstName,
                LastName = model.LastName,
                StaffStudentNumber =
                    model.StaffStudentNumber,
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
        //Approve Membership
        [HttpGet]
        public IActionResult Membership()
        {
            var memberships = _context.Memberships.ToList();
            return View(memberships);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveMembership(int membershipId)
        {
            var membership = _context.Memberships.FirstOrDefault(m => m.MembershipId == membershipId);
            if (membership == null) return NotFound();

            membership.Status = "Approved";
            _context.SaveChanges();

            TempData["Success"] = "Membership approved successfully.";
            return RedirectToAction("Membership");
        }




        [HttpGet]
        public IActionResult ApprovePayment(int paymentId)
        {
            var payment = _context.Payments.FirstOrDefault(p => p.PaymentId == paymentId);
            if (payment == null) return NotFound();

            payment.Status = "Approved";
            _context.SaveChanges();

            TempData["Success"] = "Payment approved successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult CreateAnnouncement()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateAnnouncement(Announcement announcement)
        {
            if (!ModelState.IsValid) return View(announcement);

            announcement.DatePosted = DateTime.Now;
            _context.Announcements.Add(announcement);
            _context.SaveChanges();

            TempData["Success"] = "Announcement posted successfully.";
            return RedirectToAction("Index");
        }
        public IActionResult ViewRequests()
        {
            var requests = _context.TrainerRequests
                .Where(r => r.Status == "Pending")
                .ToList();

            return View(requests);
        }
    }
}
