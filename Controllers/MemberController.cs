using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Claims;
using ZXing;
using ZXing.Common;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Student")]
    public class MemberController : Controller
    {
        private readonly GymDbContext _context;

        public MemberController(GymDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // DASHBOARD
        // =========================================================

        public IActionResult Dashboard()
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId);

            if (member == null)
            {
                return NotFound();
            }

            var membership = _context.Memberships
     .Where(m => m.MemberId == memberId)
     .OrderByDescending(m => m.EndDate)
     .FirstOrDefault();

            var attendanceCount = _context.Attendances
                .Count(a => a.MemberId == memberId);

            var reservationCount = _context.Reservations
                .Count(r =>
                    r.MemberID == memberId &&
                    r.Status == "Reserved");


            // Get this member's workout plans
            var workouts = _context.WorkoutPlans
                .Where(w => w.MemberId == memberId)
                .OrderByDescending(w => w.WorkoutPlanId)
                .ToList();

            var workoutProfile = _context.WorkoutProfiles
    .FirstOrDefault(p => p.MemberId == memberId);

            var dashboardData = new
            {
                Member = member,
                Membership = membership,
                AttendanceCount = attendanceCount,
                ReservationCount = reservationCount,
                Workouts = workouts,
                WorkoutProfile = workoutProfile
            };

            return View(dashboardData);
        }


        // =========================================================
        // PROFILE
        // =========================================================

        public IActionResult Profile()
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId);

            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        // =========================================================
        // MEMBERSHIP
        // =========================================================

        [HttpGet]
        public IActionResult Membership()
        {
            var memberIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!int.TryParse(memberIdClaim, out int memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get the latest approved membership
            var membership = _context.Memberships
                .Where(m => m.MemberId == memberId)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefault();

            // Get the student's latest pending application
            var pendingApplication = _context.MembershipApplications
                .Where(a =>
                    a.MemberId == memberId &&
                    a.Status == "Pending")
                .OrderByDescending(a => a.ApplicationDate)
                .FirstOrDefault();

            ViewBag.PendingApplication = pendingApplication;

            return View(membership);
        }
        // =========================================================
        // ATTENDANCE
        // =========================================================

        public IActionResult Attendance()
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var attendance = _context.Attendances
                .Where(a => a.MemberId == memberId)
                .OrderByDescending(a => a.CheckInTime)
                .ToList();

            return View(attendance);
        }


        // =========================================================
        // MEMBERSHIP APPLICATION - GET
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId);

            if (member == null)
            {
                return NotFound();
            }

            // Check if the member already has an active membership
            var activeMembership = _context.Memberships
                .FirstOrDefault(m =>
                    m.MemberId == memberId &&
                    m.Status == "Active" &&
                    m.EndDate.Date >= DateTime.Today);

            if (activeMembership != null)
            {
                TempData["MembershipError"] =
                    "You already have an active membership.";

                return RedirectToAction("Membership");
            }

            // Check if there is already a pending application
            var pendingApplication =
                _context.MembershipApplications
                    .FirstOrDefault(a =>
                        a.MemberId == memberId &&
                        a.Status == "Pending");

            if (pendingApplication != null)
            {
                TempData["MembershipError"] =
                    "You already have a pending membership application.";

                return RedirectToAction("Membership");
            }

            var membershipPage = new DUT_Campus_FIT_Gym.ViewModels.MembershipPage
            {
                Name = member.FirstName,
                Surname = member.LastName,
                Email = member.Email,
                StudentNo = member.StaffStudentNumber
            };

            return View(membershipPage);
        }


        // =========================================================
        // MEMBERSHIP APPLICATION - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
    DUT_Campus_FIT_Gym.ViewModels.MembershipPage membershipPage)
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId);

            if (member == null)
            {
                return NotFound();
            }

            // ==========================================
            // CHECK ACTIVE MEMBERSHIP
            // ==========================================

            var activeMembership = _context.Memberships
                .FirstOrDefault(m =>
                    m.MemberId == memberId &&
                    m.Status == "Active" &&
                    m.EndDate.Date >= DateTime.Today);

            if (activeMembership != null)
            {
                TempData["MembershipError"] =
                    "You already have an active membership.";

                return RedirectToAction("Membership");
            }


            // ==========================================
            // CHECK PENDING APPLICATION
            // ==========================================

            var pendingApplication =
                _context.MembershipApplications
                    .FirstOrDefault(a =>
                        a.MemberId == memberId &&
                        a.Status == "Pending");

            if (pendingApplication != null)
            {
                TempData["MembershipError"] =
                    "You already have a pending membership application.";

                return RedirectToAction("Membership");
            }


            // ==========================================
            // VALIDATE FORM
            // ==========================================

            if (!ModelState.IsValid)
            {
                membershipPage.Name = member.FirstName;
                membershipPage.Surname = member.LastName;
                membershipPage.Email = member.Email;
                membershipPage.StudentNo =
                    member.StaffStudentNumber;

                return View(membershipPage);
            }


            // ==========================================
            // VALIDATE MEMBERSHIP PERIOD
            // ==========================================

            if (membershipPage.MembershipPeriod != "Semester" &&
                membershipPage.MembershipPeriod != "Annual")
            {
                ModelState.AddModelError(
                    "MembershipPeriod",
                    "Please select a valid membership period.");

                membershipPage.Name = member.FirstName;
                membershipPage.Surname = member.LastName;
                membershipPage.Email = member.Email;
                membershipPage.StudentNo =
                    member.StaffStudentNumber;

                return View(membershipPage);
            }


            // ==========================================
            // DETERMINE PRICE
            // ==========================================

            decimal price;

            if (member.Role == "Student")
            {
                price = membershipPage.MembershipPeriod == "Semester"
                    ? 250m
                    : 500m;
            }
            else if (member.Role == "Staff")
            {
                price = membershipPage.MembershipPeriod == "Semester"
                    ? 300m
                    : 600m;
            }
            else
            {
                ModelState.AddModelError(
                    "",
                    "Your account is not eligible for a membership.");

                membershipPage.Name = member.FirstName;
                membershipPage.Surname = member.LastName;
                membershipPage.Email = member.Email;
                membershipPage.StudentNo = member.StaffStudentNumber;

                return View(membershipPage);
            }


            // ==========================================
            // VERIFY DOCUMENT
            // ==========================================

            if (membershipPage.VerificationDocument == null ||
                membershipPage.VerificationDocument.Length == 0)
            {
                ModelState.AddModelError(
                    "VerificationDocument",
                    "Please upload your student/staff card.");

                membershipPage.Name = member.FirstName;
                membershipPage.Surname = member.LastName;
                membershipPage.Email = member.Email;
                membershipPage.StudentNo =
                    member.StaffStudentNumber;

                return View(membershipPage);
            }


            // ==========================================
            // GET FILE EXTENSION
            // ==========================================

            string extension = System.IO.Path
                .GetExtension(
                    membershipPage.VerificationDocument!.FileName)
                .ToLowerInvariant();

            // ==========================================
            // CHECK FILE TYPE
            // ==========================================

            if (extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png" &&
                extension != ".pdf")
            {
                ModelState.AddModelError(
                    "VerificationDocument",
                    "Only JPG, JPEG, PNG and PDF files are allowed.");

                membershipPage.Name = member.FirstName;
                membershipPage.Surname = member.LastName;
                membershipPage.Email = member.Email;
                membershipPage.StudentNo = member.StaffStudentNumber;

                return View(membershipPage);
            }


            // ==========================================
            // CHECK FILE SIZE
            // Maximum 5 MB
            // ==========================================

            if (membershipPage.VerificationDocument.Length >
                5 * 1024 * 1024)
            {
                ModelState.AddModelError(
                    "VerificationDocument",
                    "The verification document must not exceed 5 MB.");

                membershipPage.Name = member.FirstName;
                membershipPage.Surname = member.LastName;
                membershipPage.Email = member.Email;
                membershipPage.StudentNo =
                    member.StaffStudentNumber;

                return View(membershipPage);
            }


            // ==========================================
            // SAVE DOCUMENT
            // ==========================================

            string uploadsFolder =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "verification");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName =
                Guid.NewGuid().ToString() + extension;

            string filePath =
                Path.Combine(
                    uploadsFolder,
                    uniqueFileName);

            using (var fileStream =
                   new FileStream(
                       filePath,
                       FileMode.Create))
            {
                membershipPage.VerificationDocument
                    .CopyTo(fileStream);
            }


            // ==========================================
            // CREATE APPLICATION
            // ==========================================

            var application = new MembershipApplication
            {
                MemberId = memberId,

                MembershipType =
                    membershipPage.MembershipPeriod,

                Price = price,

                ApplicationDate = DateTime.Now,

                Status = "Pending",

                VerificationDocument =
                    "/uploads/verification/" +
                    uniqueFileName,

                ReviewedDate = null,

                AdminComment = null
            };


            _context.MembershipApplications.Add(application);

            _context.SaveChanges();


            // ==========================================
            // SUCCESS
            // ==========================================

            TempData["MembershipSuccess"] =
                "Your membership application has been submitted successfully. Please wait for admin approval.";

            return RedirectToAction("Membership");
        }


        


        // =========================================================
        // EQUIPMENT
        // =========================================================

        public IActionResult Equipment()
        {
            var equipment = _context.Equipment.ToList();

            return View(equipment);
        }


        // =========================================================
        // RESERVATIONS
        // =========================================================

        public IActionResult Reservations()
        {
            var memberId = int.Parse(
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier)
            );

            var reservations = _context.Reservations
                .Where(r => r.MemberID == memberId)
                .Join(
                    _context.Equipment,
                    reservation => reservation.EquipmentID,
                    equipment => equipment.EquipmentID,
                    (reservation, equipment) =>
                        new ReservationViewModel
                        {
                            ReservationID =
                                reservation.ReservationID,

                            EquipmentName =
                                equipment.EquipmentName,

                            ReservationDate =
                                reservation.ReservationDate,

                            Status =
                                reservation.Status
                        }
                )
                .ToList();

            return View(reservations);
        }


        // =========================================================
        // ANNOUNCEMENTS
        // =========================================================

        public IActionResult Announcements()
        {
            var announcements = _context.Announcements
                .OrderByDescending(a => a.DatePosted)
                .ToList();

            return View(announcements);
        }


        // =========================================================
        // GYM CARD / BARCODE
        // =========================================================

        [HttpGet]
        public IActionResult GymCard()
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId);

            if (member == null)
            {
                return NotFound();
            }

            var membership = _context.Memberships
                .Where(m => m.MemberId == memberId)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefault();


            // NO MEMBERSHIP

            if (membership == null)
            {
                return View(new GymCardViewModel
                {
                    MemberId = member.MemberId,

                    FullName =
                        $"{member.FirstName} {member.LastName}",

                    StaffStudentNumber =
                        member.StaffStudentNumber,

                    Email =
                        member.Email,

                    Role =
                        member.Role,

                    Status =
                        "NO MEMBERSHIP"
                });
            }


            // MEMBERSHIP STATUS

            var status =
                membership.EndDate.Date >= DateTime.Today
                    ? "ACTIVE"
                    : "EXPIRED";


            // DAYS REMAINING

            var daysRemaining =
                Math.Max(
                    0,
                    (
                        membership.EndDate.Date -
                        DateTime.Today
                    ).Days
                );


            // BARCODE DATA
            // Only Member ID is stored in the barcode.

            var barcodeData =
                $"DUTGYM:{member.MemberId}";


            // CREATE CODE 128 BARCODE

            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.CODE_128,

                Options = new EncodingOptions
                {
                    Width = 500,
                    Height = 150,
                    Margin = 10
                }
            };

            var pixelData =
                writer.Write(barcodeData);


            // CREATE IMAGE

            using var bitmap = new Bitmap(
                pixelData.Width,
                pixelData.Height,
                PixelFormat.Format32bppRgb);

            var bitmapData =
                bitmap.LockBits(
                    new Rectangle(
                        0,
                        0,
                        pixelData.Width,
                        pixelData.Height),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppRgb);

            System.Runtime.InteropServices.Marshal.Copy(
                pixelData.Pixels,
                0,
                bitmapData.Scan0,
                pixelData.Pixels.Length);

            bitmap.UnlockBits(bitmapData);


            // CONVERT BARCODE TO BASE64

            using var stream =
                new MemoryStream();

            bitmap.Save(
                stream,
                ImageFormat.Png);

            var barcodeBase64 =
                Convert.ToBase64String(
                    stream.ToArray());


            // CREATE VIEW MODEL

            var viewModel = new GymCardViewModel
            {
                MemberId =
                    member.MemberId,

                FullName =
                    $"{member.FirstName} {member.LastName}",

                StaffStudentNumber =
                    member.StaffStudentNumber,

                Email =
                    member.Email,

                Role =
                    member.Role,

                MembershipId =
                    membership.MembershipId,

                MembershipType =
                    membership.MembershipType,

                StartDate =
                    membership.StartDate,

                EndDate =
                    membership.EndDate,

                Status =
                    status,

                DaysRemaining =
                    daysRemaining,

                Barcode =
                    barcodeBase64
            };

            return View(viewModel);
        }


        // ==========================================
        // GET: CHECK IN
        // ==========================================

        [HttpGet]
        public IActionResult CheckIn()
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId);

            if (member == null)
            {
                return NotFound();
            }

            // Get latest membership
            var membership = _context.Memberships
                .Where(m => m.MemberId == memberId)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefault();

            bool membershipActive =
                membership != null &&
                membership.EndDate.Date >= DateTime.Today;

            // Find current open attendance
            var attendance = _context.Attendances
                .Where(a =>
                    a.MemberId == memberId &&
                    a.CheckOutTime == null)
                .OrderByDescending(a => a.CheckInTime)
                .FirstOrDefault();

            var viewModel = new CheckInViewModel
            {
                FullName =
                    $"{member.FirstName} {member.LastName}",

                MembershipActive =
                    membershipActive,

                IsCheckedIn =
                    attendance != null,

                CheckInTime =
                    attendance?.CheckInTime
            };

            return View(viewModel);
        }




        // ==========================================
        // POST: VERIFY QR CODE + CHECK IN
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyQrCheckIn(string qrData)
        {
            if (string.IsNullOrWhiteSpace(qrData))
            {
                TempData["CheckInError"] =
                    "No QR code was detected.";

                return RedirectToAction(nameof(CheckIn));
            }

            // ==========================================
            // TEMPORARY QR TEST
            // ==========================================

            // Example QR data:
            //
            // Name: Asanda Mjadu
            // Number: 25081017
            // Email: 25081017@dut4life.ac.za
            // Phone: 0646470842
            // Role: Student

            var numberLine = qrData
                .Split('\n')
                .FirstOrDefault(x =>
                    x.Trim().StartsWith("Number:"));

            if (numberLine == null)
            {
                TempData["CheckInError"] =
                    "Invalid DUT FIT Gym QR code.";

                return RedirectToAction(nameof(CheckIn));
            }

            var studentNumber = numberLine
                .Substring("Number:".Length)
                .Trim();

            if (string.IsNullOrWhiteSpace(studentNumber))
            {
                TempData["CheckInError"] =
                    "Student number could not be read.";

                return RedirectToAction(nameof(CheckIn));
            }

            // ==========================================
            // FIND MEMBER
            // ==========================================

            var member = _context.Members
                .FirstOrDefault(m =>
                    m.StaffStudentNumber == studentNumber);

            if (member == null)
            {
                TempData["CheckInError"] =
                    "Member account could not be found.";

                return RedirectToAction(nameof(CheckIn));
            }

            // ==========================================
            // FIND MEMBERSHIP
            // ==========================================

            var membership = _context.Memberships
                .Where(m => m.MemberId == member.MemberId)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefault();

            if (membership == null)
            {
                TempData["CheckInError"] =
                    "You do not have a gym membership.";

                return RedirectToAction(nameof(CheckIn));
            }

            // ==========================================
            // CHECK MEMBERSHIP
            // ==========================================

            if (membership.EndDate.Date < DateTime.Today)
            {
                TempData["CheckInError"] =
                    "Your gym membership has expired.";

                return RedirectToAction(nameof(CheckIn));
            }

            // ==========================================
            // CHECK ALREADY CHECKED IN
            // ==========================================

            var existingAttendance = _context.Attendances
                .FirstOrDefault(a =>
                    a.MemberId == member.MemberId &&
                    a.CheckOutTime == null);

            if (existingAttendance != null)
            {
                TempData["CheckInError"] =
                    "You are already checked in.";

                return RedirectToAction(nameof(CheckIn));
            }

            // ==========================================
            // CREATE ATTENDANCE
            // ==========================================

            var attendance = new Attendance
            {
                MemberId = member.MemberId,
                CheckInTime = DateTime.Now,
                CheckOutTime = null
            };

            _context.Attendances.Add(attendance);

            _context.SaveChanges();

            // ==========================================
            // SUCCESS
            // ==========================================

            TempData["CheckInSuccess"] =
                $"ACCESS GRANTED — Welcome {member.FirstName}!";

            return RedirectToAction(nameof(CheckIn));
        }

        // ==========================================
        // POST: CHECK OUT
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckOut()
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            // Find the member's current open attendance
            var attendance = _context.Attendances
                .Where(a =>
                    a.MemberId == memberId &&
                    a.CheckOutTime == null)
                .OrderByDescending(a => a.CheckInTime)
                .FirstOrDefault();

            // No active check-in found
            if (attendance == null)
            {
                TempData["CheckInError"] =
                    "You are not currently checked in.";

                return RedirectToAction(nameof(CheckIn));
            }

            // Record checkout time
            attendance.CheckOutTime = DateTime.Now;

            _context.SaveChanges();

            // Success message
            TempData["CheckInSuccess"] =
                "You have successfully checked out. See you next time!";

            return RedirectToAction(nameof(CheckIn));
        }
        // ==========================================
        // REQUEST TRAINER - GET
        // ==========================================

        [HttpGet]
        public IActionResult RequestTrainer()
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            var trainers = _context.Trainers
                .OrderBy(t => t.TrainerName)
                .ToList();

            ViewBag.Trainers = trainers;

            return View();
        }


        // ==========================================
        // REQUEST TRAINER - POST
        // ==========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestTrainer(
            int trainerId,
            string requestMessage)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            int studentId = int.Parse(memberIdClaim);


            // ==========================================
            // VALIDATE MESSAGE
            // ==========================================

            if (string.IsNullOrWhiteSpace(requestMessage))
            {
                ModelState.AddModelError(
                    "",
                    "Please explain what assistance you need.");

                ViewBag.Trainers = _context.Trainers
                    .OrderBy(t => t.TrainerName)
                    .ToList();

                return View();
            }


            // ==========================================
            // FIND TRAINER
            // ==========================================

            var trainer = _context.Trainers
                .FirstOrDefault(t =>
                    t.TrainerId == trainerId);

            if (trainer == null)
            {
                ModelState.AddModelError(
                    "",
                    "Please select a valid trainer.");

                ViewBag.Trainers = _context.Trainers
                    .OrderBy(t => t.TrainerName)
                    .ToList();

                return View();
            }


            // ==========================================
            // CHECK EXISTING PENDING REQUEST
            // ==========================================

            var existingRequest =
                _context.TrainerRequests
                    .FirstOrDefault(r =>
                        r.StudentId == studentId &&
                        r.TrainerId == trainerId &&
                        r.Status == "Pending");

            if (existingRequest != null)
            {
                ModelState.AddModelError(
                    "",
                    "You already have a pending request with this trainer.");

                ViewBag.Trainers = _context.Trainers
                    .OrderBy(t => t.TrainerName)
                    .ToList();

                return View();
            }


            // ==========================================
            // CREATE REQUEST
            // ==========================================

            var request = new TrainerRequest
            {
                StudentId = studentId,

                TrainerId = trainer.TrainerId,

                RequestMessage = requestMessage.Trim(),

                Status = "Pending",

                RequestDate = DateTime.Now
            };


            _context.TrainerRequests.Add(request);

            _context.SaveChanges();


            // ==========================================
            // SUCCESS
            // ==========================================

            TempData["TrainerRequestSuccess"] =
                "Your trainer assistance request has been sent.";

            return RedirectToAction("Dashboard");
        }
        // =========================================================
        // MY TRAINER REQUESTS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> MyTrainerRequests()
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            if (!int.TryParse(memberIdClaim, out int memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            var requests = await _context.TrainerRequests
                .Include(r => r.Trainer)
                .Where(r => r.StudentId == memberId)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }
    }
}