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
    }
}
