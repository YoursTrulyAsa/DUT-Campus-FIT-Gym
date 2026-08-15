using DUT_Campus_FIT_Gym.Data;
using System.Linq;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text.RegularExpressions;

namespace DUT_Campus_FIT_Gym.Controllers
{
    public class AccountController : Controller
    {
        private readonly GymDbContext _context;
        private readonly PasswordHasher<Member> _passwordHasher;

        public AccountController(GymDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Member>();
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
            if (model.Role == "Student")
            {
                string studentPattern = @"^[0-9]{8}@dut4life\.ac\.za$";
                if (!Regex.IsMatch(model.Email, studentPattern, RegexOptions.IgnoreCase))
                {
                    ModelState.AddModelError("Email",
                        "Student email must start with an 8-digit student number and end with @dut4life.ac.za");
                    return View(model);
                }
            }
            else if (model.Role == "Staff")
            {
                string staffPattern = @"^[A-Za-z]+@dut\.ac\.za$";
                if (!Regex.IsMatch(model.Email, staffPattern, RegexOptions.IgnoreCase))
                {
                    ModelState.AddModelError("Email",
                        "Staff email must start with a name and end with @dut.ac.za");
                    return View(model);
                }
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
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login");
        }
    }
}
