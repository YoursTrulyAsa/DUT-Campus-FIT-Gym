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
                .Where(m => m.MemberId == memberId)
                .OrderByDescending(m => m.MembershipId)
                .FirstOrDefault();

            var attendanceCount = _context.Attendances
                .Count(a => a.MemberId == memberId);

            var reservationCount = _context.Reservations
                .Count(r =>
                    r.MemberID == memberId &&
                    r.Status == "Reserved");

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
        // PROFILE - GET
        // =========================================================

        [HttpGet]
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
        // PROFILE - POST / UPDATE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(
            string name,
            string surname,
            string phoneNumber)
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

            // =====================================================
            // VALIDATE EDITABLE FIELDS
            // =====================================================

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(
                    "Name",
                    "Name is required.");
            }

            if (string.IsNullOrWhiteSpace(surname))
            {
                ModelState.AddModelError(
                    "Surname",
                    "Surname is required.");
            }

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                ModelState.AddModelError(
                    "PhoneNumber",
                    "Phone number is required.");
            }

            // =====================================================
            // IF INVALID
            // =====================================================

            if (!ModelState.IsValid)
            {
                return View(member);
            }

            // =====================================================
            // UPDATE ONLY ALLOWED FIELDS
            // =====================================================

            member.Name = name.Trim();

            member.Surname = surname.Trim();

            member.PhoneNumber = phoneNumber.Trim();

            _context.SaveChanges();

            TempData["ProfileSuccess"] =
                "Your profile has been updated successfully.";

            return RedirectToAction(nameof(Profile));
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
                .Where(m => m.MemberId == memberId)
                .OrderByDescending(m => m.MembershipId)
                .FirstOrDefault();

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

            // =====================================================
            // CHECK CURRENT MEMBERSHIP
            // =====================================================

            var currentMembership = _context.Memberships
                .Where(m => m.MemberId == memberId)
                .OrderByDescending(m => m.MembershipId)
                .FirstOrDefault();

            if (currentMembership != null)
            {
                // Active membership
                if (currentMembership.Status == "Active" &&
                    currentMembership.EndDate.HasValue &&
                    currentMembership.EndDate.Value.Date >= DateTime.Today)
                {
                    TempData["MembershipError"] =
                        "You already have an active membership.";

                    return RedirectToAction(
                        nameof(Membership));
                }

                // Pending membership
                if (currentMembership.Status == "Pending")
                {
                    TempData["MembershipError"] =
                        "You already have a membership application awaiting admin approval.";

                    return RedirectToAction(
                        nameof(Membership));
                }

                // Waiting for payment
                if (currentMembership.Status == "WaitingForPayment")
                {
                    TempData["MembershipError"] =
                        "You already have an approved membership waiting for payment.";

                    return RedirectToAction(
                        nameof(Membership));
                }
            }

            // =====================================================
            // CHECK WHETHER STUDENT HAS EVER HAD A MEMBERSHIP
            // =====================================================

            var hasPreviousMembership = _context.Memberships
                .Any(m => m.MemberId == memberId);

            // =====================================================
            // FIRST-TIME MEMBER VALUE COMES FROM DATABASE
            // =====================================================

            membershipPage.First_Time_Member =
                !hasPreviousMembership;

            // =====================================================
            // VALIDATE FORM
            // =====================================================

            if (!ModelState.IsValid)
            {
                membershipPage.Name = member.Name;
                membershipPage.Surname = member.Surname;
                membershipPage.Email = member.Email;
                membershipPage.StudentNo = member.StudentNumber;

                return View(membershipPage);
            }

            // =====================================================
            // DETERMINE MEMBERSHIP PRICE
            // =====================================================

            decimal price = membershipPage.payments_plan switch
            {
                MembershipPage.PAY.Monthly => 150m,
                MembershipPage.PAY.Quarterly => 400m,
                MembershipPage.PAY.Half_Yearly => 700m,
                MembershipPage.PAY.Annually => 1200m,
                _ => 0m
            };

            // =====================================================
            // FIRST-TIME MEMBER DISCOUNT
            // =====================================================

            decimal discount = 0m;

            if (membershipPage.First_Time_Member)
            {
                discount = price * 0.10m;
            }

            decimal finalPrice = price - discount;

            // =====================================================
            // CREATE MEMBERSHIP
            // =====================================================

            var membership = new Membership
            {
                MemberId = memberId,

                MembershipType =
                    membershipPage.payments_plan.ToString(),

                PaymentMethod =
                    membershipPage.Payment_Method,

                FirstTimeMember =
                    membershipPage.First_Time_Member,

                StartDate = null,

                EndDate = null,

                Status = "Pending",

                Price = finalPrice,

                PaymentStatus = null,

                PaymentReference = null,

                PaymentDate = null
            };

            _context.Memberships.Add(membership);

            _context.SaveChanges();

            TempData["MembershipSuccess"] =
                "Your membership application has been submitted and is awaiting admin approval.";

            return RedirectToAction(
                nameof(Membership));
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
                .OrderByDescending(m => m.MembershipId)
                .FirstOrDefault();

            if (membership == null)
            {
                return View(new GymCardViewModel
                {
                    MemberId = member.MemberId,
                    FullName = $"{member.Name} {member.Surname}",
                    StaffStudentNumber = member.StudentNumber,
                    Email = member.Email,
                    Role = member.Role,
                    Status = "NO MEMBERSHIP"
                });
            }

            var status =
                membership.Status == "Active" &&
                membership.EndDate.HasValue &&
                membership.EndDate.Value.Date >= DateTime.Today
                    ? "ACTIVE"
                    : "EXPIRED";

            var daysRemaining = 0;

            if (membership.EndDate.HasValue)
            {
                daysRemaining = Math.Max(
                    0,
                    (
                        membership.EndDate.Value.Date -
                        DateTime.Today
                    ).Days);
            }

            var barcodeData =
                $"DUTGYM:{member.MemberId}";

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

            var pixelData = writer.Write(barcodeData);

            using var bitmap = new Bitmap(
                pixelData.Width,
                pixelData.Height,
                PixelFormat.Format32bppRgb);

            var bitmapData = bitmap.LockBits(
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

            using var stream = new MemoryStream();

            bitmap.Save(
                stream,
                ImageFormat.Png);

            var barcodeBase64 =
                Convert.ToBase64String(
                    stream.ToArray());

            var viewModel = new GymCardViewModel
            {
                MemberId = member.MemberId,
                FullName = $"{member.Name} {member.Surname}",
                StaffStudentNumber = member.StudentNumber,
                Email = member.Email,
                Role = member.Role,
                MembershipId = membership.MembershipId,
                MembershipType = membership.MembershipType,
                StartDate =
                    membership.StartDate ??
                    DateTime.MinValue,
                EndDate =
                    membership.EndDate ??
                    DateTime.MinValue,
                Status = status,
                DaysRemaining = daysRemaining,
                Barcode = barcodeBase64
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
                .OrderByDescending(m => m.MembershipId)
                .FirstOrDefault();

            bool membershipActive =
                membership != null &&
                membership.EndDate.HasValue &&
                membership.EndDate.Value.Date >= DateTime.Today &&
                membership.Status == "Active";

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

            var member = _context.Members
                .FirstOrDefault(m =>
                    m.StudentNumber == studentNumber);

            if (member == null)
            {
                TempData["CheckInError"] =
                    "Member account could not be found.";

                return RedirectToAction(nameof(CheckIn));
            }

            var membership = _context.Memberships
                .Where(m => m.MemberId == member.MemberId)
                .OrderByDescending(m => m.MembershipId)
                .FirstOrDefault();

            if (membership == null)
            {
                TempData["CheckInError"] =
                    "You do not have a gym membership.";

                return RedirectToAction(nameof(CheckIn));
            }

            if (membership.Status != "Active" ||
                !membership.EndDate.HasValue ||
                membership.EndDate.Value.Date < DateTime.Today)
            {
                TempData["CheckInError"] =
                    "Your gym membership is not active.";

                return RedirectToAction(nameof(CheckIn));
            }

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

            var attendance = new Attendance
            {
                MemberId = member.MemberId,
                CheckInTime = DateTime.Now,
                CheckOutTime = null
            };

            _context.Attendances.Add(attendance);

            _context.SaveChanges();

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

            var attendance = _context.Attendances
                .Where(a =>
                    a.MemberId == memberId &&
                    a.CheckOutTime == null)
                .OrderByDescending(a => a.CheckInTime)
                .FirstOrDefault();

            if (attendance == null)
            {
                TempData["CheckInError"] =
                    "You are not currently checked in.";

                return RedirectToAction(nameof(CheckIn));
            }

            attendance.CheckOutTime =
                DateTime.Now;

            _context.SaveChanges();

            TempData["CheckInSuccess"] =
                "You have successfully checked out. See you next time!";

            return RedirectToAction(nameof(CheckIn));
        }


        // =========================================================
        // REQUEST TRAINER - GET
        // =========================================================

        [HttpGet]
        public IActionResult RequestTrainer()
        {
            var memberIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

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


        // =========================================================
        // REQUEST TRAINER - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RequestTrainer(
            int trainerId,
            string requestMessage)
        {
            var memberIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            int studentId =
                int.Parse(memberIdClaim);

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