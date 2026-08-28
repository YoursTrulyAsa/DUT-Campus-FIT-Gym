using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Security.Claims;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Student,Staff")]
    public class MemberController : Controller
    {
        private readonly GymDbContext _context;

        public MemberController(GymDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET MEMBER ID
        // =========================================================

        private int? GetMemberId()
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim))
            {
                return null;
            }

            if (!int.TryParse(memberIdClaim, out int memberId))
            {
                return null;
            }

            return memberId;
        }

        // =========================================================
        // DASHBOARD
        // =========================================================

        [HttpGet]
        public IActionResult Dashboard()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId.Value);

            if (member == null)
            {
                return NotFound();
            }

            var latestMembership = _context.Memberships
                .Where(m => m.MemberId == memberId.Value)
                .OrderByDescending(m => m.MembershipId)
                .FirstOrDefault();

            var latestApplication = _context.MembershipApplications
                .Where(a => a.MemberId == memberId.Value)
                .OrderByDescending(a => a.MembershipApplicationId)
                .FirstOrDefault();

            var attendanceCount = _context.Attendances
                .Count(a => a.MemberId == memberId.Value);

            var reservationCount = _context.Reservations
                .Count(r =>
                    r.MemberID == memberId.Value &&
                    r.Status == "Reserved" &&
                    r.EndTime > DateTime.Now);

            var workouts = _context.WorkoutPlans
                .Where(w => w.MemberId == memberId.Value)
                .OrderBy(w => w.WorkoutDay)
                .ToList();

            var workoutProfile = _context.WorkoutProfiles
                .FirstOrDefault(w => w.MemberId == memberId.Value);

            var dashboardData = new
            {
                Member = member,
                Membership = latestMembership,
                LatestApplication = latestApplication,
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

        [HttpGet]
        public IActionResult Profile()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = _context.Members
                .FirstOrDefault(m =>
                    m.MemberId == memberId.Value);

            if (member == null)
            {
                return NotFound();
            }

            return View(member);
        }

        // =========================================================
        // UPDATE PROFILE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(
            string name,
            string surname,
            string phoneNumber)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId.Value);

            if (member == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(
                    "Name",
                    "First name is required.");
            }

            if (string.IsNullOrWhiteSpace(surname))
            {
                ModelState.AddModelError(
                    "Surname",
                    "Last name is required.");
            }

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                ModelState.AddModelError(
                    "PhoneNumber",
                    "Phone number is required.");
            }

            if (!ModelState.IsValid)
            {
                return View(member);
            }

            member.Name = name.Trim();
            member.Surname = surname.Trim();
            member.PhoneNumber = phoneNumber.Trim();

            _context.SaveChanges();

            TempData["ProfileSuccess"] =
                "Your profile has been updated successfully.";

            return RedirectToAction(nameof(Profile));
        }

        // =========================================================
        // ATTENDANCE
        // =========================================================

        [HttpGet]
        public IActionResult Attendance()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var attendance = _context.Attendances
                .Where(a =>
                    a.MemberId == memberId.Value)
                .OrderByDescending(a =>
                    a.CheckInTime)
                .ToList();

            return View(attendance);
        }

        // =========================================================
        // PAYMENT HISTORY
        // =========================================================

        [HttpGet]
        public IActionResult PaymentHistory()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var payments = _context.Payments
                .Include(p => p.Membership)
                .Where(p => p.MemberId == memberId.Value)
                .OrderByDescending(p => p.PaymentDate)
                .ToList();

            return View(payments);
        }

        // =========================================================
        // ANNOUNCEMENTS
        // =========================================================

        [HttpGet]
        public IActionResult Announcements()
        {
            var announcements = _context.Announcements
                .OrderByDescending(a =>
                    a.DatePosted)
                .ToList();

            return View(announcements);
        }

        // =========================================================
        // MEMBERSHIP PAGE
        // =========================================================

        [HttpGet]
        public IActionResult Membership()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var latestMembership = _context.Memberships
                .Where(m => m.MemberId == memberId.Value)
                .OrderByDescending(m => m.MembershipId)
                .FirstOrDefault();

            var latestApplication = _context.MembershipApplications
                .Where(a => a.MemberId == memberId.Value)
                .OrderByDescending(a => a.MembershipApplicationId)
                .FirstOrDefault();

            if (latestMembership != null &&
                latestMembership.Status == "WaitingForPayment")
            {
                ViewBag.PendingApplication = null;

                return View(latestMembership);
            }

            if (latestMembership != null &&
                latestMembership.Status == "Active")
            {
                ViewBag.PendingApplication = null;

                return View(latestMembership);
            }

            if (latestApplication != null &&
                latestApplication.Status == "Pending")
            {
                ViewBag.PendingApplication = latestApplication;

                return View(null);
            }

            if (latestApplication != null &&
                latestApplication.Status == "Approved")
            {
                ViewBag.PendingApplication = latestApplication;

                return View(latestMembership);
            }

            if (latestApplication != null &&
                latestApplication.Status == "Rejected")
            {
                ViewBag.PendingApplication = latestApplication;

                return View(null);
            }

            ViewBag.PendingApplication = null;

            return View(null);
        }

        // =========================================================
        // MEMBERSHIP APPLICATION - GET
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId.Value);

            if (member == null)
            {
                return NotFound();
            }

            var latestMembership = _context.Memberships
                .Where(m => m.MemberId == memberId.Value)
                .OrderByDescending(m => m.MembershipId)
                .FirstOrDefault();

            if (latestMembership != null &&
                latestMembership.Status == "Active" &&
                latestMembership.EndDate.HasValue &&
                latestMembership.EndDate.Value.Date >= DateTime.Today)
            {
                TempData["MembershipError"] =
                    "You already have an active membership.";

                return RedirectToAction(nameof(Membership));
            }

            if (latestMembership != null &&
                latestMembership.Status == "WaitingForPayment")
            {
                TempData["MembershipError"] =
                    "Your membership has been approved and is waiting for payment.";

                return RedirectToAction(nameof(Membership));
            }

            var pendingApplication = _context.MembershipApplications
                .Where(a =>
                    a.MemberId == memberId.Value &&
                    a.Status == "Pending")
                .OrderByDescending(a => a.MembershipApplicationId)
                .FirstOrDefault();

            if (pendingApplication != null)
            {
                TempData["MembershipError"] =
                    "You already have a pending membership application.";

                return RedirectToAction(nameof(Membership));
            }

            bool isFirstTimeMember =
                !_context.MembershipApplications
                    .Any(a => a.MemberId == memberId.Value)
                &&
                !_context.Memberships
                    .Any(m => m.MemberId == memberId.Value);

            ViewBag.IsFirstTimeMember = isFirstTimeMember;

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
        // MEMBERSHIP APPLICATION - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            MembershipPage membershipPage)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = _context.Members
                .FirstOrDefault(m =>
                    m.MemberId == memberId.Value);

            if (member == null)
            {
                return NotFound();
            }

            var activeMembership =
                _context.Memberships
                    .FirstOrDefault(m =>
                        m.MemberId == memberId.Value &&
                        m.Status == "Active" &&
                        m.EndDate.HasValue &&
                        m.EndDate.Value.Date >= DateTime.Today);

            if (activeMembership != null)
            {
                TempData["MembershipError"] =
                    "You already have an active membership.";

                return RedirectToAction(nameof(Membership));
            }

            var existingApplication =
                _context.MembershipApplications
                    .FirstOrDefault(a =>
                        a.MemberId == memberId.Value &&
                        (
                            a.Status == "Pending" ||
                            a.Status == "WaitingForPayment"
                        ));

            if (existingApplication != null)
            {
                if (existingApplication.Status ==
                    "Pending")
                {
                    TempData["MembershipError"] =
                        "You already have a membership application awaiting admin approval.";
                }
                else
                {
                    TempData["MembershipError"] =
                        "Your membership has already been approved and is waiting for payment.";
                }

                return RedirectToAction(nameof(Membership));
            }

            if (membershipPage.MembershipPeriod !=
                    "Semester" &&
                membershipPage.MembershipPeriod !=
                    "Annual")
            {
                ModelState.AddModelError(
                    "MembershipPeriod",
                    "Please select a valid membership period.");
            }

            if (string.IsNullOrWhiteSpace(
                membershipPage.PaymentMethod))
            {
                ModelState.AddModelError(
                    "PaymentMethod",
                    "Please select a payment method.");
            }

            bool isFirstTimeMember =
                !_context.MembershipApplications
                    .Any(a =>
                        a.MemberId == memberId.Value)
                &&
                !_context.Memberships
                    .Any(m =>
                        m.MemberId == memberId.Value);

            decimal basePrice = 0m;

            switch (membershipPage.MembershipPeriod)
            {
                case "Semester":
                    basePrice = 150m;
                    break;

                case "Annual":
                    basePrice = 300m;
                    break;
            }

            decimal discount = 0m;
            decimal price = basePrice;

            if (isFirstTimeMember &&
                basePrice > 0)
            {
                discount =
                    basePrice * 0.10m;

                price =
                    basePrice - discount;
            }

            if (price <= 0)
            {
                ModelState.AddModelError(
                    "MembershipPeriod",
                    "Please select a valid membership period.");
            }

            if (membershipPage.VerificationDocument ==
                null ||
                membershipPage.VerificationDocument.Length ==
                0)
            {
                ModelState.AddModelError(
                    "VerificationDocument",
                    "Please upload your student/staff card.");
            }
            else
            {
                string extension =
                    Path.GetExtension(
                        membershipPage.VerificationDocument.FileName)
                        .ToLowerInvariant();

                if (extension != ".jpg" &&
                    extension != ".jpeg" &&
                    extension != ".png" &&
                    extension != ".pdf")
                {
                    ModelState.AddModelError(
                        "VerificationDocument",
                        "Only JPG, JPEG, PNG and PDF files are allowed.");
                }

                if (membershipPage.VerificationDocument.Length >
                    5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "VerificationDocument",
                        "The verification document must not exceed 5 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                membershipPage.Name =
                    member.Name;

                membershipPage.Surname =
                    member.Surname;

                membershipPage.Email =
                    member.Email;

                membershipPage.StudentNo =
                    member.StudentNumber;

                ViewBag.IsFirstTimeMember =
                    isFirstTimeMember;

                return View(membershipPage);
            }

            string documentExtension =
                Path.GetExtension(
                    membershipPage.VerificationDocument!.FileName)
                    .ToLowerInvariant();

            string uploadsFolder =
                Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "verification");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(
                    uploadsFolder);
            }

            string uniqueFileName =
                Guid.NewGuid().ToString() +
                documentExtension;

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

            var application =
                new MembershipApplication
                {
                    MemberId =
                        memberId.Value,

                    MembershipType =
                        membershipPage.MembershipPeriod!,

                    Price =
                        price,

                    ApplicationDate =
                        DateTime.Now,

                    Status =
                        "Pending",

                    VerificationDocument =
                        "/uploads/verification/" +
                        uniqueFileName,

                    PaymentMethod =
                        membershipPage.PaymentMethod!
                };

            _context.MembershipApplications
                .Add(application);

            _context.SaveChanges();

            if (isFirstTimeMember)
            {
                TempData["MembershipSuccess"] =
                    $"Your membership application has been submitted. " +
                    $"As a first-time member, your 10% discount has been applied. " +
                    $"Your membership fee is R{price:0.00}.";
            }
            else
            {
                TempData["MembershipSuccess"] =
                    $"Your membership application has been submitted. " +
                    $"Your membership fee is R{price:0.00}.";
            }

            return RedirectToAction(
                nameof(Membership));
        }

        // =========================================================
        // VIRTUAL GYM CARD
        // =========================================================

        [HttpGet]
        public IActionResult GymCard()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var member = _context.Members
                .FirstOrDefault(m =>
                    m.MemberId == memberId.Value);

            if (member == null)
            {
                return NotFound();
            }

            var membership =
                _context.Memberships
                    .Where(m =>
                        m.MemberId == memberId.Value &&
                        m.Status == "Active" &&
                        m.EndDate.HasValue &&
                        m.EndDate.Value.Date >= DateTime.Today)
                    .OrderByDescending(m =>
                        m.MembershipId)
                    .FirstOrDefault();

            var viewModel =
                new GymCardViewModel
                {
                    MemberId =
                        member.MemberId,

                    FullName =
                        $"{member.Name} {member.Surname}",

                    Role =
                        member.Role,

                    StaffStudentNumber =
                        member.StudentNumber,

                    Email =
                        member.Email,

                    MembershipId =
                        membership?.MembershipId ?? 0,

                    MembershipType =
                        membership?.MembershipType ??
                        "No Active Membership",

                    StartDate =
                        membership?.StartDate ??
                        DateTime.MinValue,

                    EndDate =
                        membership?.EndDate ??
                        DateTime.MinValue,

                    DaysRemaining =
                        membership != null &&
                        membership.EndDate.HasValue
                            ? Math.Max(
                                0,
                                (
                                    membership.EndDate.Value.Date -
                                    DateTime.Today
                                ).Days)
                            : 0,

                    Status =
                        membership?.Status?.ToUpper() ??
                        "INACTIVE"
                };

            if (membership != null &&
                !string.IsNullOrWhiteSpace(member.StudentNumber))
            {
                string barcodeValue =
                    member.StudentNumber;

                var writer =
                    new BarcodeWriterPixelData
                    {
                        Format = BarcodeFormat.CODE_128,

                        Options = new EncodingOptions
                        {
                            Width = 500,
                            Height = 120,
                            Margin = 10,
                            PureBarcode = true
                        }
                    };

                var pixelData =
                    writer.Write(barcodeValue);

                using var memoryStream =
                    new MemoryStream();

                using (var bitmap =
                    new System.Drawing.Bitmap(
                        pixelData.Width,
                        pixelData.Height,
                        System.Drawing.Imaging.PixelFormat.Format32bppRgb))
                {
                    var bitmapData =
                        bitmap.LockBits(
                            new System.Drawing.Rectangle(
                                0,
                                0,
                                bitmap.Width,
                                bitmap.Height),
                            System.Drawing.Imaging.ImageLockMode.WriteOnly,
                            System.Drawing.Imaging.PixelFormat.Format32bppRgb);

                    try
                    {
                        System.Runtime.InteropServices.Marshal.Copy(
                            pixelData.Pixels,
                            0,
                            bitmapData.Scan0,
                            pixelData.Pixels.Length);
                    }
                    finally
                    {
                        bitmap.UnlockBits(bitmapData);
                    }

                    bitmap.Save(
                        memoryStream,
                        System.Drawing.Imaging.ImageFormat.Png);
                }

                viewModel.Barcode =
                    Convert.ToBase64String(
                        memoryStream.ToArray());
            }

            return View(viewModel);
        }

        // =========================================================
        // CHECK-IN
        // =========================================================

        [HttpGet]
        public IActionResult CheckIn()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var member = _context.Members
                .FirstOrDefault(m =>
                    m.MemberId == memberId.Value);

            if (member == null)
            {
                return NotFound();
            }

            var activeMembership = _context.Memberships
                .Where(m =>
                    m.MemberId == memberId.Value &&
                    m.Status == "Active" &&
                    m.EndDate.HasValue &&
                    m.EndDate.Value.Date >= DateTime.Today)
                .OrderByDescending(m =>
                    m.MembershipId)
                .FirstOrDefault();

            var currentAttendance = _context.Attendances
                .Where(a =>
                    a.MemberId == memberId.Value &&
                    a.CheckOutTime == null)
                .OrderByDescending(a =>
                    a.CheckInTime)
                .FirstOrDefault();

            var viewModel = new CheckInViewModel
            {
                FullName =
                    $"{member.Name} {member.Surname}",

                MembershipActive =
                    activeMembership != null,

                IsCheckedIn =
                    currentAttendance != null,

                CheckInTime =
                    currentAttendance?.CheckInTime
            };

            return View(viewModel);
        }

        // =========================================================
        // OLD CHECK-IN ROUTE
        // =========================================================

        [HttpGet]
        public IActionResult CheckInPage()
        {
            return RedirectToAction(
                nameof(CheckIn));
        }

        // =========================================================
        // CHECK-IN RESULT
        // =========================================================

        [HttpGet]
        public IActionResult CheckInResult()
        {
            return View();
        }

        // =========================================================
        // CHECK OUT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CheckOut()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var attendance = _context.Attendances
                .Where(a =>
                    a.MemberId == memberId.Value &&
                    a.CheckOutTime == null)
                .OrderByDescending(a =>
                    a.CheckInTime)
                .FirstOrDefault();

            if (attendance == null)
            {
                TempData["CheckInError"] =
                    "No active check-in was found.";

                return RedirectToAction(
                    nameof(CheckIn));
            }

            attendance.CheckOutTime =
                DateTime.Now;

            _context.SaveChanges();

            TempData["CheckInSuccess"] =
                "You have successfully checked out of the gym.";

            return RedirectToAction(
                nameof(CheckIn));
        }

        // =========================================================
        // QR CHECK-IN
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyQrCheckIn(string qrData)
        {
            if (string.IsNullOrWhiteSpace(qrData))
            {
                TempData["CheckInError"] =
                    "No gym QR code was detected.";

                return RedirectToAction(nameof(CheckIn));
            }

            qrData = qrData.Trim();

            if (!string.Equals(
                qrData,
                "DUTGYM_CHECKIN",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["CheckInError"] =
                    "Invalid DUT Campus FIT Gym QR code.";

                return RedirectToAction(nameof(CheckIn));
            }

            var memberId = GetMemberId();

            if (memberId == null)
            {
                TempData["CheckInError"] =
                    "Your account could not be identified. Please log in again.";

                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var member = _context.Members
                .FirstOrDefault(m =>
                    m.MemberId == memberId.Value);

            if (member == null)
            {
                TempData["CheckInError"] =
                    "Your member account could not be found.";

                return RedirectToAction(nameof(CheckIn));
            }

            var membership = _context.Memberships
                .Where(m =>
                    m.MemberId == memberId.Value &&
                    m.Status == "Active")
                .OrderByDescending(m =>
                    m.MembershipId)
                .FirstOrDefault();

            if (membership == null)
            {
                TempData["CheckInError"] =
                    "You do not have an active gym membership.";

                return RedirectToAction(nameof(CheckIn));
            }

            if (membership.EndDate.HasValue &&
                membership.EndDate.Value.Date < DateTime.Today)
            {
                TempData["CheckInError"] =
                    "Your gym membership has expired.";

                return RedirectToAction(nameof(CheckIn));
            }

            var existingAttendance = _context.Attendances
                .FirstOrDefault(a =>
                    a.MemberId == memberId.Value &&
                    a.CheckOutTime == null);

            if (existingAttendance != null)
            {
                TempData["CheckInError"] =
                    "You are already checked in.";

                return RedirectToAction(nameof(CheckIn));
            }

            var attendance = new Attendance
            {
                MemberId = memberId.Value,
                CheckInTime = DateTime.Now,
                CheckOutTime = null
            };

            _context.Attendances.Add(attendance);

            _context.SaveChanges();

            TempData["CheckInSuccess"] =
                $"Access granted — Welcome {member.Name}!";

            return RedirectToAction(nameof(CheckIn));
        }

        // =========================================================
        // MEMBER EQUIPMENT PAGE
        // =========================================================

        [HttpGet]
        public IActionResult Equipment()
        {
            var equipment =
                _context.Equipment
                    .OrderBy(e => e.EquipmentName)
                    .ToList();

            return View(equipment);
        }

        // =========================================================
        // MEMBER RESERVATIONS
        // =========================================================

        [HttpGet]
        public IActionResult Reservations()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var now = DateTime.Now;

            // -----------------------------------------------------
            // AUTOMATICALLY EXPIRE OLD RESERVATIONS
            // -----------------------------------------------------

            var expiredReservations = _context.Reservations
                .Where(r =>
                    r.MemberID == memberId.Value &&
                    r.Status == "Reserved" &&
                    r.EndTime <= now)
                .ToList();

            foreach (var expired in expiredReservations)
            {
                expired.Status = "Expired";

                var equipment =
                    _context.Equipment
                        .FirstOrDefault(e =>
                            e.EquipmentID ==
                            expired.EquipmentID);

                if (equipment != null)
                {
                    equipment.IsAvailable = true;
                }
            }

            if (expiredReservations.Any())
            {
                _context.SaveChanges();
            }

            // -----------------------------------------------------
            // LOAD RESERVATIONS
            // IMPORTANT: EndTime IS INCLUDED
            // -----------------------------------------------------

            var reservations =
                (
                    from reservation
                    in _context.Reservations

                    join equipment
                    in _context.Equipment

                    on reservation.EquipmentID
                    equals equipment.EquipmentID

                    where reservation.MemberID ==
                            memberId.Value

                    orderby reservation.ReservationDate
                        descending

                    select new ReservationViewModel
                    {
                        ReservationID =
                            reservation.ReservationID,

                        MemberID =
                            reservation.MemberID,

                        EquipmentName =
                            equipment.EquipmentName,

                        ReservationDate =
                            reservation.ReservationDate,

                        EndTime =
                            reservation.EndTime,

                        Status =
                            reservation.Status
                    }
                )
                .ToList();

            return View(reservations);
        }

        // =========================================================
        // UNRESERVE EQUIPMENT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Unreserve(
            int reservationId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var reservation =
                _context.Reservations
                    .FirstOrDefault(r =>
                        r.ReservationID ==
                            reservationId &&
                        r.MemberID ==
                            memberId.Value &&
                        r.Status ==
                            "Reserved");

            if (reservation == null)
            {
                TempData["EquipmentError"] =
                    "The reservation could not be found or has already expired.";

                return RedirectToAction(
                    nameof(Reservations));
            }

            var equipment =
                _context.Equipment
                    .FirstOrDefault(e =>
                        e.EquipmentID ==
                        reservation.EquipmentID);

            reservation.Status =
                "Cancelled";

            if (equipment != null)
            {
                equipment.IsAvailable =
                    true;
            }

            _context.SaveChanges();

            TempData["EquipmentSuccess"] =
                "Equipment reservation cancelled successfully.";

            return RedirectToAction(
                nameof(Reservations));
        }

        // =========================================================
        // REQUEST TRAINER - GET
        // =========================================================

        [HttpGet]
        public IActionResult RequestTrainer()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId.Value);

            if (member == null)
            {
                return NotFound();
            }

            var trainers = _context.Trainers
                .OrderBy(t => t.TrainerName)
                .ToList();

            if (!trainers.Any())
            {
                TempData["TrainerRequestError"] =
                    "There are currently no trainers available.";

                return RedirectToAction(
                    nameof(MyTrainerRequests));
            }

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
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId.Value);

            if (member == null)
            {
                return NotFound();
            }

            var trainer = _context.Trainers
                .FirstOrDefault(t => t.TrainerId == trainerId);

            if (trainer == null)
            {
                TempData["TrainerRequestError"] =
                    "The selected trainer could not be found.";

                return RedirectToAction(
                    nameof(RequestTrainer));
            }

            if (string.IsNullOrWhiteSpace(requestMessage))
            {
                TempData["TrainerRequestError"] =
                    "Please explain what you need help with.";

                return RedirectToAction(
                    nameof(RequestTrainer));
            }

            requestMessage =
                requestMessage.Trim();

            if (requestMessage.Length > 500)
            {
                TempData["TrainerRequestError"] =
                    "Your request message cannot exceed 500 characters.";

                return RedirectToAction(
                    nameof(RequestTrainer));
            }

            var activeRequest =
                _context.TrainerRequests
                    .Any(r =>
                        r.StudentId ==
                            memberId.Value &&
                        (
                            r.Status ==
                                "Pending" ||
                            r.Status ==
                                "Accepted"
                        ));

            if (activeRequest)
            {
                TempData["TrainerRequestError"] =
                    "You already have a pending or active trainer request.";

                return RedirectToAction(
                    nameof(MyTrainerRequests));
            }

            var trainerRequest =
                new TrainerRequest
                {
                    StudentId =
                        memberId.Value,

                    TrainerId =
                        trainerId,

                    RequestMessage =
                        requestMessage,

                    Status =
                        "Pending",

                    RequestDate =
                        DateTime.Now
                };

            _context.TrainerRequests.Add(
                trainerRequest);

            _context.SaveChanges();

            TempData["TrainerRequestSuccess"] =
                $"Your request has been sent to {trainer.TrainerName}.";

            return RedirectToAction(
                nameof(MyTrainerRequests));
        }

        // =========================================================
        // MY TRAINER REQUESTS
        // =========================================================

        [HttpGet]
        public IActionResult MyTrainerRequests()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var requests =
                _context.TrainerRequests
                    .Include(r => r.Trainer)
                    .Where(r =>
                        r.StudentId ==
                        memberId.Value)
                    .OrderByDescending(r =>
                        r.RequestDate)
                    .ToList();

            return View(requests);
        }

        // =========================================================
        // PAYMENT
        // =========================================================

        [HttpGet]
        public IActionResult Payment()
        {
            var memberIdClaim =
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier);

            if (memberIdClaim == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            if (!int.TryParse(
                memberIdClaim.Value,
                out int memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var payments = _context.Payments
                .Include(p => p.Membership)
                .Where(p =>
                    p.MemberId ==
                    memberId)
                .OrderByDescending(p =>
                    p.PaymentDate)
                .ToList();

            return View(
                "Payment",
                payments);
        }
    }
}