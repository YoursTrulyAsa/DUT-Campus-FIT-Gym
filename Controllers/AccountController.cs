using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MimeKit;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;


namespace DUT_Campus_FIT_Gym.Controllers
{
    public class AccountController : Controller
    {
        private readonly GymDbContext _context;
        private readonly PasswordHasher<Member> _passwordHasher;
        private readonly IConfiguration _config;

        public AccountController(
            GymDbContext context,
            IConfiguration config)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Member>();
            _config = config;
        }

        // =========================
        // REGISTER
        // =========================

        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Role = new SelectList(
                new[]
                {
                "Student",
                 "Stuff"

                }
            );

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
                    ModelState.AddModelError("Email", "Student email must start with an 8-digit student number and end with @dut4life.ac.za");
                    return View(model);
                }

                else if (model.Role == "Staff")
                {
                    string staffPattern = @"^[A-Za-z]+@dut\.ac\.za$";
                    if (!Regex.IsMatch(model.Email, staffPattern, RegexOptions.IgnoreCase))
                    {
                        ModelState.AddModelError("Email", "Staff email must start with a name and end with @dut.ac.za");
                        return View(model);
                    }
                }

                bool emailExists = _context.Members
                    .Any(m =>
                        m.Email.ToLower() ==
                        model.Email.Trim().ToLower());

                if (emailExists)
                {
                    ModelState.AddModelError(
                        "Email",
                        "This email is already registered.");

                    return View(model);
                }

                // Check if student number already exists
                bool numberExists = _context.Members
                    .Any(m =>
                        m.StaffStudentNumber ==
                        model.StudentNumber);

                if (numberExists)
                {
                    ModelState.AddModelError(
                        "StudentNumber",
                        "This Student Number is already registered.");

                    return View(model);
                }

                // Create student account
                var member = new Member
                {
                    FirstName = model.Name,
                    LastName = model.Surname,
                    StaffStudentNumber = model.StudentNumber,
                    Email = model.Email.Trim().ToLower(),
                    PhoneNumber = model.PhoneNumber,
                    Role = model.Role
                };

                // Hash password
                member.PasswordHash =
                    _passwordHasher.HashPassword(
                        member,
                        model.Password);

                // Save member
                _context.Members.Add(member);
                _context.SaveChanges();

                // Send confirmation email
                SendRegistrationEmail(member);

                TempData["RegistrationSuccess"] =
                    "Your account has been created successfully. A confirmation email has been sent to your email address.";

                return RedirectToAction("Login");
            }
            return RedirectToAction("Login", "Account");
        }

        // =========================
        // REGISTRATION EMAIL
        // =========================

        private void SendRegistrationEmail(Member member)
        {
            var smtpSettings =
                _config.GetSection("SmtpSettings");

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    "DUT Campus FIT Gym",
                    smtpSettings["SenderEmail"]));

            message.To.Add(
                new MailboxAddress(
                    $"{member.FirstName} {member.LastName}",
                    member.Email));

            message.Subject =
                "Welcome to DUT Campus FIT Gym";

            var builder = new BodyBuilder();

            builder.TextBody =
                $"Hello {member.FirstName},\n\n" +
                "Welcome to DUT Campus FIT Gym!\n\n" +
                "Your account has been successfully created.\n\n" +
                $"Member ID: {member.MemberId}\n" +
                $"Name: {member.FirstName} {member.LastName}\n" +
                $"Student Number: {member.StaffStudentNumber}\n" +
                $"Email: {member.Email}\n\n" +
                "You can now log in to the DUT Campus FIT Gym system using your registered email address and password.\n\n" +
                "Your Virtual Gym Card will be available from your account after logging in.\n\n" +
                "Regards,\n" +
                "DUT Campus FIT Gym";

            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();

            client.ServerCertificateValidationCallback =
                (s, c, h, e) => true;

            client.Connect(
                smtpSettings["Server"],
                int.Parse(smtpSettings["Port"]),
                SecureSocketOptions.StartTls);

            client.Authenticate(
                smtpSettings["SenderEmail"],
                smtpSettings["Password"]);

            client.Send(message);

            client.Disconnect(true);
        }


        // =========================
        // LOGIN
        // =========================

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var member = _context.Members
                .FirstOrDefault(m =>
                    m.Email == model.Email);

            if (member == null ||
                _passwordHasher.VerifyHashedPassword(
                    member,
                    member.PasswordHash,
                    model.Password)
                == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    member.MemberId.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    $"{member.FirstName} {member.LastName}"),

                new Claim(
                    ClaimTypes.Email,
                    member.Email),

                new Claim(
                    ClaimTypes.Role,
                    member.Role),

                new Claim("PhoneNumber", member.PhoneNumber ?? "")
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal =
                new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);

            if (member.Role == "Admin")
            {
                return RedirectToAction("Index", "Admin");
            }

            if (member.Role == "Trainer")
            {
                return RedirectToAction("Index", "Trainer");
            }

            return RedirectToAction("Dashboard", "Member");
        }


        // =========================
        // EQUIPMENT RESERVATION
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reserve(int id)
        {
            var memberId = int.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier));

            var equipment =
                _context.Equipment.Find(id);

            if (equipment == null)
            {
                return NotFound();
            }

            if (!equipment.IsAvailable)
            {
                return RedirectToAction(
                    "Index",
                    "Equipment");
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

            return RedirectToAction(
                "Index",
                "Equipment");
        }


        // =========================
        // FORGOT PASSWORD
        // =========================

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(
            ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var member = _context.Members
                .FirstOrDefault(m =>
                    m.Email == model.Email);

            if (member == null)
            {
                ModelState.AddModelError(
                    "",
                    "No account was found with that email address.");

                return View(model);
            }

            string otp =
                RandomNumberGenerator
                    .GetInt32(100000, 1000000)
                    .ToString();

            DateTime expiry =
                DateTime.Now.AddMinutes(10);

            TempData["ResetEmail"] =
                member.Email;

            TempData["ResetOtp"] =
                otp;

            TempData["ResetOtpExpiry"] =
                expiry.ToString("O");

            SendPasswordResetOtp(
                member,
                otp);

            return RedirectToAction(
                "VerifyOtp");
        }


        // =========================
        // PASSWORD RESET EMAIL
        // =========================

        private void SendPasswordResetOtp(
            Member member,
            string otp)
        {
            var smtpSettings =
                _config.GetSection("SmtpSettings");

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    "DUT Campus FIT Gym",
                    smtpSettings["SenderEmail"]));

            message.To.Add(
                new MailboxAddress(
                    $"{member.FirstName} {member.LastName}",
                    member.Email));

            message.Subject =
                "DUT Campus FIT Gym - Password Reset OTP";

            var builder = new BodyBuilder();

            builder.TextBody =
                $"Hello {member.FirstName},\n\n" +
                "We received a request to reset your DUT Campus FIT Gym password.\n\n" +
                $"Your verification code is: {otp}\n\n" +
                "This code will expire in 10 minutes.\n\n" +
                "If you did not request a password reset, you can ignore this email.\n\n" +
                "Regards,\n" +
                "DUT Campus FIT Gym";

            message.Body =
                builder.ToMessageBody();

            using var client =
                new SmtpClient();

            client.ServerCertificateValidationCallback =
                (s, c, h, e) => true;

            client.Connect(
                smtpSettings["Server"],
                int.Parse(smtpSettings["Port"]),
                SecureSocketOptions.StartTls);

            client.Authenticate(
                smtpSettings["SenderEmail"],
                smtpSettings["Password"]);

            client.Send(message);

            client.Disconnect(true);
        }


        // =========================
        // VERIFY OTP
        // =========================

        [HttpGet]
        public IActionResult VerifyOtp()
        {
            if (TempData["ResetEmail"] == null ||
                TempData["ResetOtp"] == null ||
                TempData["ResetOtpExpiry"] == null)
            {
                return RedirectToAction(
                    "ForgotPassword");
            }

            TempData.Keep("ResetEmail");
            TempData.Keep("ResetOtp");
            TempData.Keep("ResetOtpExpiry");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(string otp)
        {
            var resetEmail =
                TempData.Peek("ResetEmail")?.ToString();

            var storedOtp =
                TempData.Peek("ResetOtp")?.ToString();

            var expiryString =
                TempData.Peek("ResetOtpExpiry")?.ToString();

            if (string.IsNullOrEmpty(resetEmail) ||
                string.IsNullOrEmpty(storedOtp) ||
                string.IsNullOrEmpty(expiryString))
            {
                return RedirectToAction(
                    "ForgotPassword");
            }

            if (!DateTime.TryParse(
                expiryString,
                out DateTime expiry))
            {
                return RedirectToAction(
                    "ForgotPassword");
            }

            if (DateTime.Now > expiry)
            {
                TempData.Clear();

                TempData["OtpError"] =
                    "Your OTP has expired. Please request a new one.";

                return RedirectToAction(
                    "ForgotPassword");
            }

            if (string.IsNullOrWhiteSpace(otp) ||
                otp != storedOtp)
            {
                TempData.Keep("ResetEmail");
                TempData.Keep("ResetOtp");
                TempData.Keep("ResetOtpExpiry");

                ViewData["OtpError"] =
                    "The OTP you entered is incorrect.";

                return View();
            }

            TempData["ResetVerified"] =
                "true";

            TempData.Keep("ResetEmail");

            TempData.Remove("ResetOtp");
            TempData.Remove("ResetOtpExpiry");

            return RedirectToAction(
                "ResetPassword");
        }


        // =========================
        // RESET PASSWORD
        // =========================

        [HttpGet]
        public IActionResult ResetPassword()
        {
            var verified =
                TempData.Peek("ResetVerified")?.ToString();

            var resetEmail =
                TempData.Peek("ResetEmail")?.ToString();

            if (verified != "true" ||
                string.IsNullOrEmpty(resetEmail))
            {
                return RedirectToAction(
                    "ForgotPassword");
            }

            TempData.Keep("ResetVerified");
            TempData.Keep("ResetEmail");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(
            string newPassword,
            string confirmPassword)
        {
            var verified =
                TempData.Peek("ResetVerified")?.ToString();

            var resetEmail =
                TempData.Peek("ResetEmail")?.ToString();

            if (verified != "true" ||
                string.IsNullOrEmpty(resetEmail))
            {
                return RedirectToAction(
                    "ForgotPassword");
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData.Keep("ResetVerified");
                TempData.Keep("ResetEmail");

                ModelState.AddModelError(
                    "",
                    "Please enter a new password.");

                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData.Keep("ResetVerified");
                TempData.Keep("ResetEmail");

                ModelState.AddModelError(
                    "",
                    "Passwords do not match.");

                return View();
            }

            var member = _context.Members
                .FirstOrDefault(m =>
                    m.Email == resetEmail);

            if (member == null)
            {
                TempData.Clear();

                return RedirectToAction(
                    "Login");
            }

            member.PasswordHash =
                _passwordHasher.HashPassword(
                    member,
                    newPassword);

            _context.SaveChanges();

            TempData.Clear();

            TempData["PasswordResetSuccess"] =
                "Your password has been reset successfully.";

            return RedirectToAction(
                "Login");
        }


        // =========================
        // LOGOUT
        // =========================

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
