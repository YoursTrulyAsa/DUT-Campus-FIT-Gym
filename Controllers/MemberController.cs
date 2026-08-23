using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;
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
                .FirstOrDefault(m => m.MemberId == memberId);

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

        public IActionResult Membership()
        {
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var membership = _context.Memberships
                .FirstOrDefault(m => m.MemberId == memberId);

            if (membership == null)
            {
                return NotFound(
                    "No membership found for this member.");
            }

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
        // CREATE MEMBERSHIP - GET
        // =========================================================

        [HttpGet]
        public IActionResult Create()
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

            var membershipPage = new MembershipPage
            {
                Name = member.Name,
                Surname = member.Surname,
                Email = member.Email,
                StudentNo = member.StudentNumber
            };

            return View(membershipPage);
        }


        // =========================================================
        // CREATE MEMBERSHIP - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(MembershipPage membershipPage)
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

            // Check if member already has membership

            var existingMembership = _context.Memberships
                .FirstOrDefault(m => m.MemberId == memberId);

            if (existingMembership != null)
            {
                ModelState.AddModelError(
                    "",
                    "You already have a membership.");

                membershipPage.Name = member.Name;
                membershipPage.Surname = member.Surname;
                membershipPage.Email = member.Email;
                membershipPage.StudentNo = member.StudentNumber;

                return View(membershipPage);
            }


            // Validate form

            if (!ModelState.IsValid)
            {
                membershipPage.Name = member.Name;
                membershipPage.Surname = member.Surname;
                membershipPage.Email = member.Email;
                membershipPage.StudentNo = member.StudentNumber;

                return View(membershipPage);
            }


            // Determine membership price

            decimal price = membershipPage.payments_plan switch
            {
                MembershipPage.PAY.Monthly => 150m,

                MembershipPage.PAY.Quarterly => 400m,

                MembershipPage.PAY.Half_Yearly => 700m,

                MembershipPage.PAY.Annually => 1200m,

                _ => 0m
            };


            // First-time member discount

            decimal discount = 0m;

            if (membershipPage.First_Time_Member)
            {
                discount = price * 0.10m;
            }

            decimal finalPrice = price - discount;


            // Membership dates

            var startDate = DateTime.Today;

            var endDate = membershipPage.payments_plan switch
            {
                MembershipPage.PAY.Monthly =>
                    startDate.AddMonths(1).AddDays(-1),

                MembershipPage.PAY.Quarterly =>
                    startDate.AddMonths(3).AddDays(-1),

                MembershipPage.PAY.Half_Yearly =>
                    startDate.AddMonths(6).AddDays(-1),

                MembershipPage.PAY.Annually =>
                    startDate.AddYears(1).AddDays(-1),

                _ => startDate
            };


            // Create membership

            var membership = new Membership
            {
                MemberId = memberId,

                MembershipType =
                    membershipPage.payments_plan.ToString(),

                PaymentMethod =
                    membershipPage.Payment_Method,

                FirstTimeMember =
                    membershipPage.First_Time_Member,

                StartDate = startDate,

                EndDate = endDate,

                Status = "Active",

                Price = finalPrice
            };

            _context.Memberships.Add(membership);

            _context.SaveChanges();

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
            var memberIdClaim = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

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
                        })
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
                        $"{member.Name} {member.Surname}",

                    StaffStudentNumber =
                        member.StudentNumber,

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


            // CONVERT TO BASE64

            using var stream = new MemoryStream();

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
                    $"{member.Name} {member.Surname}",

                StaffStudentNumber =
                    member.StudentNumber,

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


        // =========================================================
        // CHECK IN - GET
        // =========================================================

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

            var membership = _context.Memberships
                .Where(m => m.MemberId == memberId)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefault();

            bool membershipActive =
                membership != null &&
                membership.EndDate.Date >= DateTime.Today;

            var attendance = _context.Attendances
                .Where(a =>
                    a.MemberId == memberId &&
                    a.CheckOutTime == null)
                .OrderByDescending(a => a.CheckInTime)
                .FirstOrDefault();

            var viewModel = new CheckInViewModel
            {
                FullName =
                    $"{member.Name} {member.Surname}",

                MembershipActive =
                    membershipActive,

                IsCheckedIn =
                    attendance != null,

                CheckInTime =
                    attendance?.CheckInTime
            };

            return View(viewModel);
        }


        // =========================================================
        // VERIFY QR CODE + CHECK IN
        // =========================================================

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


            // TEMPORARY QR TEST

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

            var studentNumber =
                numberLine
                    .Substring("Number:".Length)
                    .Trim();

            if (string.IsNullOrWhiteSpace(studentNumber))
            {
                TempData["CheckInError"] =
                    "Student number could not be read.";

                return RedirectToAction(nameof(CheckIn));
            }


            // FIND MEMBER

            var member = _context.Members
                .FirstOrDefault(m =>
                    m.StudentNumber == studentNumber);

            if (member == null)
            {
                TempData["CheckInError"] =
                    "Member account could not be found.";

                return RedirectToAction(nameof(CheckIn));
            }


            // FIND MEMBERSHIP

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


            // CHECK MEMBERSHIP

            if (membership.EndDate.Date < DateTime.Today)
            {
                TempData["CheckInError"] =
                    "Your gym membership has expired.";

                return RedirectToAction(nameof(CheckIn));
            }


            // CHECK ALREADY CHECKED IN

            var existingAttendance =
                _context.Attendances
                    .FirstOrDefault(a =>
                        a.MemberId == member.MemberId &&
                        a.CheckOutTime == null);

            if (existingAttendance != null)
            {
                TempData["CheckInError"] =
                    "You are already checked in.";

                return RedirectToAction(nameof(CheckIn));
            }


            // CREATE ATTENDANCE

            var attendance = new Attendance
            {
                MemberId =
                    member.MemberId,

                CheckInTime =
                    DateTime.Now,

                CheckOutTime =
                    null
            };

            _context.Attendances.Add(attendance);

            _context.SaveChanges();


            // SUCCESS

            TempData["CheckInSuccess"] =
                $"ACCESS GRANTED — Welcome {member.Name}!";

            return RedirectToAction(nameof(CheckIn));
        }


        // =========================================================
        // CHECK OUT
        // =========================================================

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


            // Find current attendance

            var attendance = _context.Attendances
                .Where(a =>
                    a.MemberId == memberId &&
                    a.CheckOutTime == null)
                .OrderByDescending(a => a.CheckInTime)
                .FirstOrDefault();


            // No active check-in

            if (attendance == null)
            {
                TempData["CheckInError"] =
                    "You are not currently checked in.";

                return RedirectToAction(nameof(CheckIn));
            }


            // Record checkout

            attendance.CheckOutTime =
                DateTime.Now;

            _context.SaveChanges();


            // Success

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

            var trainers = _context.Members
                .Where(m => m.Role == "Trainer")
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

            // Check that selected trainer exists
            var trainer = _context.Members
                .FirstOrDefault(m =>
                    m.MemberId == trainerId &&
                    m.Role == "Trainer");

            if (trainer == null)
            {
                ModelState.AddModelError(
                    "",
                    "Please select a valid trainer.");

                ViewBag.Trainers = _context.Members
                    .Where(m => m.Role == "Trainer")
                    .ToList();

                return View();
            }

            if (string.IsNullOrWhiteSpace(requestMessage))
            {
                ModelState.AddModelError(
                    "",
                    "Please explain what assistance you need.");

                ViewBag.Trainers = _context.Members
                    .Where(m => m.Role == "Trainer")
                    .ToList();

                return View();
            }

            // Check if there is already a pending request
            var existingRequest = _context.TrainerRequests
                .FirstOrDefault(r =>
                    r.StudentId == studentId &&
                    r.TrainerId == trainerId &&
                    r.Status == "Pending");

            if (existingRequest != null)
            {
                ModelState.AddModelError(
                    "",
                    "You already have a pending request with this trainer.");

                ViewBag.Trainers = _context.Members
                    .Where(m => m.Role == "Trainer")
                    .ToList();

                return View();
            }

            var request = new TrainerRequest
            {
                StudentId = studentId,
                TrainerId = trainerId,
                RequestMessage = requestMessage,
                Status = "Pending",
                RequestDate = DateTime.Now
            };

            _context.TrainerRequests.Add(request);

            _context.SaveChanges();

            TempData["TrainerRequestSuccess"] =
                "Your trainer assistance request has been sent.";

            return RedirectToAction("Dashboard");
        }
    }
}