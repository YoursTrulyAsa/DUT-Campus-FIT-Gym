using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using QRCoder;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using static Org.BouncyCastle.Math.EC.ECCurve;


namespace DUT_Campus_FIT_Gym.Controllers
{
    public class AccountController : Controller
    {
        private readonly GymDbContext _context;
        private readonly PasswordHasher<Member> _passwordHasher;
        private readonly IConfiguration _config;
        

        public AccountController(GymDbContext context, IConfiguration config)
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
            bool emailExists = _context.Members.Any(m => m.Email == model.Email);
            bool numberExists = _context.Members.Any(m => m.StaffStudentNumber == model.StaffStudentNumber);

            if (emailExists)
            {
                ModelState.AddModelError("Email", "This email is already registered.");
                return View(model);
            }

            if (numberExists)
            {
                ModelState.AddModelError("StaffStudentNumber", "This student/staff number is already registered.");
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
            member.PasswordHash = _passwordHasher.HashPassword(member, model.Password);
            _context.Members.Add(member);
            _context.SaveChanges();

            string qrData = $"Name: {member.FirstName} {member.LastName}\n" +
                    $"Number: {member.StaffStudentNumber}\n" +
                    $"Email: {member.Email}\n" +
                    $"Phone: {member.PhoneNumber}\n" +
                    $"Role: {member.Role}";

            using (var qrGenerator = new QRCodeGenerator())
            using (var qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q))
            using (var qrCode = new QRCode(qrCodeData))
            using (var bitmap = qrCode.GetGraphic(20))
            using (var stream = new MemoryStream())
            {
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;
                var qrBytes = stream.ToArray();

                // Send email with QR code
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("DUT Campus FIT Gym", "yourgym@example.com"));
                message.To.Add(new MailboxAddress(member.FirstName, member.Email));
                message.Subject = "Welcome to DUT Campus FIT Gym";

                var builder = new BodyBuilder
                {
                    TextBody = $"Hello {member.FirstName},\n\nWelcome to DUT Campus FIT Gym! " +
                               $"Attached is your QR code containing your registration details."
                };

                builder.Attachments.Add("QRCode.png", qrBytes, new ContentType("image", "png"));
                message.Body = builder.ToMessageBody();

                var smtpSettings = _config.GetSection("SmtpSettings");
                using (var client = new SmtpClient())
                {
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true; // bypass SSL revocation issue
                    client.Connect(smtpSettings["Server"], int.Parse(smtpSettings["Port"]), MailKit.Security.SecureSocketOptions.StartTls);
                    client.Authenticate(smtpSettings["SenderEmail"], smtpSettings["Password"]);
                    client.Send(message);
                    client.Disconnect(true);
                }
            }

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

            if (member == null || _passwordHasher.VerifyHashedPassword(member, member.PasswordHash, model.Password)
                 == PasswordVerificationResult.Failed)
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
