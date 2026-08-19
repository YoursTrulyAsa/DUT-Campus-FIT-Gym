using DUT_Campus_FIT_Gym.Data;
using System.Linq;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace DUT_Campus_FIT_Gym.Controllers
{
    public class AccountController : Controller
    {
        private readonly GymDbContext _context;

        public AccountController(GymDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var member = new Member
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                StaffStudentNumber = model.StaffStudentNumber,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Role = model.Role,
                PasswordHash = model.Password
            };

            _context.Members.Add(member);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var member = _context.Members
                .FirstOrDefault(m => m.Email == model.Email);

            if (member == null || member.PasswordHash != model.Password)
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            var claims = new List<Claim>
    {
        new Claim(
            ClaimTypes.NameIdentifier,
            member.MemberId.ToString()
        ),

        new Claim(
            ClaimTypes.Name,
            member.FirstName
        ),

        new Claim(
            ClaimTypes.Email,
            member.Email
        ),

        new Claim(
            ClaimTypes.Role,
            member.Role
        )
    };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            return RedirectToAction("Dashboard", "Member");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reserve(int id)
        {
            var memberId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
            );

            var equipment = _context.Equipment.Find(id);

            if (equipment == null)
            {
                return NotFound();
            }

            if (!equipment.IsAvailable)
            {
                return RedirectToAction(nameof(Index));
            }

            var reservation = new Reservation
            {
                MemberID = memberId,
                EquipmentID = equipment.EquipmentID,
                ReservationDate = DateTime.Now,
                Status = "Reserved"
            };

            equipment.IsAvailable = false;

            _context.Reservations.Add(reservation);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var member = _context.Members
                .FirstOrDefault(m => m.Email == model.Email);

            if (member == null)
            {
                ModelState.AddModelError("", "No account was found with that email address.");
                return View(model);
            }

            TempData["ResetEmail"] = member.Email;

            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (TempData["ResetEmail"] == null)
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(
            string email,
            string newPassword,
            string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError("", "Please enter a new password.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View();
            }

            var member = _context.Members
                .FirstOrDefault(m => m.Email == email);

            if (member == null)
            {
                return RedirectToAction("Login");
            }

            member.PasswordHash = newPassword;

            _context.SaveChanges();

            TempData["PasswordResetSuccess"] = "Your password has been reset successfully.";

            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login");
        }
    }
}
