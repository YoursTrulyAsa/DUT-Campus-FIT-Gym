using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Security.Claims;
using System.Security.Cryptography;

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

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string email =
                model.Email?.Trim().ToLower() ?? "";

            string studentNumber =
                model.StudentNumber?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Email address is required.");

                return View(model);
            }

            if (string.IsNullOrWhiteSpace(studentNumber))
            {
                ModelState.AddModelError(
                    "StudentNumber",
                    "Student Number is required.");

                return View(model);
            }

            string expectedStudentEmail =
                $"{studentNumber}@dut4life.ac.za";


            if (!string.Equals(
                email,
                expectedStudentEmail,
                StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(
                    "Email",
                    "Your email must match your Student Number and use @dut4life.ac.za.");

                return View(model);
            }

            bool emailExists =
                await _context.Members
                    .AnyAsync(m =>
                        m.Email.ToLower() == email);


            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered.");

                return View(model);
            }

            bool numberExists =
                await _context.Members
                    .AnyAsync(m =>
                        m.StudentNumber == studentNumber);


            if (numberExists)
            {
                ModelState.AddModelError(
                    "StudentNumber",
                    "This Student Number is already registered.");

                return View(model);
            }

            var member = new Member
            {
                Name =
                    model.Name?.Trim() ?? "",

                Surname =
                    model.Surname?.Trim() ?? "",

                StudentNumber =
                    studentNumber,

                Email =
                    email,

                PhoneNumber =
                    model.PhoneNumber?.Trim() ?? "",

                Role =
                    "Student"
            };

            member.PasswordHash =
                _passwordHasher.HashPassword(
                    member,
                    model.Password);


            try
            {
                _context.Members.Add(member);

                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "The account could not be created. Please try again.");

                return View(model);
            }


            // =====================================================
            // SEND REGISTRATION EMAIL
            // =====================================================
            //
            // IMPORTANT:
            //
            // Email failure must NOT prevent registration.
            //
            // The account has already been successfully saved.
            //
            // =====================================================

            try
            {
                await SendRegistrationEmail(member);
            }
            catch (Exception)
            {
                // Do NOT delete the account.
                //
                // Registration has succeeded.
                //
                // Email is optional and can fail because of:
                //
                // SMTP
                // Internet
                // certificate
                // authentication
                // configuration
                //
            }


            TempData["RegistrationSuccess"] =
                "Your account has been created successfully. You can now log in.";

            return RedirectToAction(
                nameof(Login));
        }

        private async Task SendRegistrationEmail(
            Member member)
        {
            var smtpSettings =
                _config.GetSection("SmtpSettings");


            string server =
                smtpSettings["Server"] ?? "";

            string portValue =
                smtpSettings["Port"] ?? "587";

            string senderEmail =
                smtpSettings["SenderEmail"] ?? "";

            string password =
                smtpSettings["Password"] ?? "";


            if (string.IsNullOrWhiteSpace(server) ||
                string.IsNullOrWhiteSpace(senderEmail) ||
                string.IsNullOrWhiteSpace(password))
            {
                return;
            }


            if (!int.TryParse(
                portValue,
                out int port))
            {
                port = 587;
            }


            var message =
                new MimeMessage();


            message.From.Add(
                new MailboxAddress(
                    "DUT Campus FIT Gym",
                    senderEmail));


            message.To.Add(
                new MailboxAddress(
                    $"{member.Name} {member.Surname}",
                    member.Email));


            message.Subject =
                "Welcome to DUT Campus FIT Gym";


            var builder =
                new BodyBuilder();


            builder.TextBody =
                $"Hello {member.Name},\n\n" +
                "Welcome to DUT Campus FIT Gym!\n\n" +
                "Your account has been successfully created.\n\n" +
                $"Member ID: {member.MemberId}\n" +
                $"Name: {member.Name} {member.Surname}\n" +
                $"Student Number: {member.StudentNumber}\n" +
                $"Email: {member.Email}\n\n" +
                "You can now log in to the DUT Campus FIT Gym system using your registered email address and password.\n\n" +
                "Your Virtual Gym Card will be available from your account after logging in.\n\n" +
                "Regards,\n" +
                "DUT Campus FIT Gym";


            message.Body =
                builder.ToMessageBody();


            using var client =
                new SmtpClient();


            client.ServerCertificateValidationCallback =
                (s, c, h, e) => true;

            await client.ConnectAsync(
                server,
                port,
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                senderEmail,
                password);

            await client.SendAsync(message);

            await client.DisconnectAsync(true);
        }

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


            string email =
                model.Email?.Trim().ToLower() ?? "";


            var member =
                await _context.Members
                    .FirstOrDefaultAsync(
                        m => m.Email.ToLower() == email);


            if (member == null)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View(model);
            }


            if (string.IsNullOrWhiteSpace(
                member.PasswordHash))
            {
                ModelState.AddModelError(
                    "",
                    "This account does not have a valid password. Please reset your password.");

                return View(model);
            }


            var passwordResult =
                _passwordHasher.VerifyHashedPassword(
                    member,
                    member.PasswordHash,
                    model.Password);


            if (passwordResult ==
                PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    "",
                    "Invalid email or password.");

                return View(model);
            }

            var claims =
                new List<Claim>
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        member.MemberId.ToString()),

                    new Claim(
                        ClaimTypes.Name,
                        member.Name ?? ""),

                    new Claim(
                        ClaimTypes.Email,
                        member.Email ?? ""),

                    new Claim(
                        ClaimTypes.Role,
                        member.Role ?? "Student")
                };


            var identity =
                new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);


            var principal =
                new ClaimsPrincipal(identity);


            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);


            if (string.Equals(
                member.Role,
                "Admin",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Index",
                    "Admin");
            }


            if (string.Equals(
                member.Role,
                "Trainer",
                StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(
                    "Index",
                    "Trainer");
            }

            return RedirectToAction(
                "Dashboard",
                "Member");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reserve(int id)
        {
            var memberIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);


            if (string.IsNullOrEmpty(memberIdClaim))
            {
                return RedirectToAction(
                    nameof(Login));
            }


            if (!int.TryParse(
                memberIdClaim,
                out int memberId))
            {
                return RedirectToAction(
                    nameof(Login));
            }


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


            var reservation =
                new Reservation
                {
                    MemberID =
                        memberId,

                    EquipmentID =
                        equipment.EquipmentID,

                    ReservationDate =
                        DateTime.Now,

                    Status =
                        "Reserved"
                };


            equipment.IsAvailable =
                false;


            _context.Reservations.Add(
                reservation);


            _context.SaveChanges();


            return RedirectToAction(
                "Index",
                "Equipment");
        }

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


            var member =
                _context.Members
                    .FirstOrDefault(
                        m => m.Email.ToLower() ==
                             model.Email.Trim().ToLower());


            if (member == null)
            {
                ModelState.AddModelError(
                    "",
                    "No account was found with that email address.");

                return View(model);
            }


            string otp =
                RandomNumberGenerator
                    .GetInt32(
                        100000,
                        1000000)
                    .ToString();


            DateTime expiry =
                DateTime.Now.AddMinutes(10);


            TempData["ResetEmail"] =
                member.Email;

            TempData["ResetOtp"] =
                otp;

            TempData["ResetOtpExpiry"] =
                expiry.ToString("O");


            try
            {
                SendPasswordResetOtp(
                    member,
                    otp);
            }
            catch
            {
                TempData.Clear();

                ModelState.AddModelError(
                    "",
                    "We could not send the password reset email. Please try again.");

                return View(model);
            }


            return RedirectToAction(
                "VerifyOtp");
        }

        private void SendPasswordResetOtp(
            Member member,
            string otp)
        {
            var smtpSettings =
                _config.GetSection("SmtpSettings");


            var message =
                new MimeMessage();


            message.From.Add(
                new MailboxAddress(
                    "DUT Campus FIT Gym",
                    smtpSettings["SenderEmail"]));


            message.To.Add(
                new MailboxAddress(
                    $"{member.Name} {member.Surname}",
                    member.Email));


            message.Subject =
                "DUT Campus FIT Gym - Password Reset OTP";


            var builder =
                new BodyBuilder();


            builder.TextBody =
                $"Hello {member.Name},\n\n" +
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
        public IActionResult VerifyOtp(
            string otp)
        {
            var resetEmail =
                TempData.Peek(
                    "ResetEmail")?.ToString();


            var storedOtp =
                TempData.Peek(
                    "ResetOtp")?.ToString();


            var expiryString =
                TempData.Peek(
                    "ResetOtpExpiry")?.ToString();


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


        [HttpGet]
        public IActionResult ResetPassword()
        {
            var verified =
                TempData.Peek(
                    "ResetVerified")?.ToString();


            var resetEmail =
                TempData.Peek(
                    "ResetEmail")?.ToString();


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
                TempData.Peek(
                    "ResetVerified")?.ToString();


            var resetEmail =
                TempData.Peek(
                    "ResetEmail")?.ToString();


            if (verified != "true" ||
                string.IsNullOrEmpty(resetEmail))
            {
                return RedirectToAction(
                    "ForgotPassword");
            }


            if (string.IsNullOrWhiteSpace(
                newPassword))
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


            var member =
                _context.Members
                    .FirstOrDefault(
                        m => m.Email == resetEmail);


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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);


            return RedirectToAction(
                "Login");
        }
    }
}