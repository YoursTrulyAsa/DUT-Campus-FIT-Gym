using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.Services;
using DUT_Campus_FIT_Gym.ViewModels;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using System.Security.Claims;
using MailKit.Net.Smtp;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Student,Staff")]
    public class MemberController : Controller
    {
        private readonly GymDbContext _context;
        private readonly MembershipPricingService _membershipPricingService;
        private readonly RewardService _rewardService;
        private readonly IConfiguration _config;

        public MemberController(
            GymDbContext context,
            MembershipPricingService membershipPricingService,
            RewardService rewardService,
            IConfiguration config)
        {
            _context = context;
            _membershipPricingService = membershipPricingService;
            _rewardService = rewardService;
            _config = config;
        }
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

            var attendance = _context.Attendances
                .Where(a => a.MemberId == memberId.Value)
                .OrderByDescending(a => a.CheckInTime)
                .ToList();

            var attendanceCount = attendance.Count;

            var completedAttendance = attendance
                .Where(a => a.CheckOutTime.HasValue)
                .ToList();

            var totalGymMinutes = completedAttendance.Sum(a =>
                (a.CheckOutTime!.Value - a.CheckInTime).TotalMinutes);

            var averageVisitMinutes = completedAttendance.Any()
                ? completedAttendance.Average(a =>
                    (a.CheckOutTime!.Value - a.CheckInTime).TotalMinutes)
                : 0;

            var today = DateTime.Today;

            var monday = today.AddDays(
                -((7 + (int)today.DayOfWeek -
                (int)DayOfWeek.Monday) % 7));

            var weeklyLabels = new List<string>();
            var weeklyVisitCounts = new List<int>();

            for (int i = 0; i < 7; i++)
            {
                var day = monday.AddDays(i);

                weeklyLabels.Add(day.ToString("ddd"));

                weeklyVisitCounts.Add(
                    attendance.Count(a =>
                        a.CheckInTime.Date == day));
            }

            var weeklyGymMinutes = attendance
                .Where(a =>
                    a.CheckInTime.Date >= monday &&
                    a.CheckInTime.Date <= today &&
                    a.CheckOutTime.HasValue)
                .Sum(a =>
                    (a.CheckOutTime!.Value - a.CheckInTime).TotalMinutes);

            var workoutProfile = _context.WorkoutProfiles
                .FirstOrDefault(w =>
                    w.MemberId == memberId.Value);

            var weightHistory = _context.WeightHistories
                .Where(w =>
                    w.MemberId == memberId.Value)
                .OrderBy(w =>
                    w.RecordedAt)
                .ToList();

            var workouts = _context.WorkoutPlans
                .Where(w =>
                    w.MemberId == memberId.Value)
                .OrderBy(w =>
                    w.WorkoutDay)
                .ToList();

            var workoutCompletions = _context.WorkoutCompletions
                .Where(w =>
                    w.MemberId == memberId.Value)
                .OrderByDescending(w =>
                    w.CompletedAt)
                .ToList();

            var totalWorkoutDays = workouts
                .Select(w => new
                {
                    w.WorkoutProgrammeId,
                    w.WeekNumber,
                    w.WorkoutDay
                })
                .Distinct()
                .Count();

            var completedWorkoutDays = workoutCompletions
                .Select(w => new
                {
                    w.WorkoutProgrammeId,
                    w.WeekNumber,
                    w.WorkoutDay
                })
                .Distinct()
                .Count();

            var workoutCompletionPercentage =
                totalWorkoutDays > 0
                    ? (int)Math.Round(
                        completedWorkoutDays * 100.0 /
                        totalWorkoutDays)
                    : 0;

            double caloriesPerMinute = 5.0;

            if (workoutProfile != null)
            {
                var weightFactor = Math.Clamp(
                    workoutProfile.Weight / 70.0,
                    0.75,
                    1.5);

                caloriesPerMinute *= weightFactor;
            }

            var estimatedCalories =
                totalGymMinutes * caloriesPerMinute;

            var weeklyEstimatedCalories =
                weeklyGymMinutes * caloriesPerMinute;

            var reservationCount = _context.Reservations
                .Count(r =>
                    r.MemberID == memberId.Value &&
                    r.Status == "Reserved" &&
                    r.EndTime > DateTime.Now);

            var totalReservations = _context.Reservations
                .Count(r =>
                    r.MemberID == memberId.Value);

            var dashboardData = new MemberDashboardViewModel
            {
                Member = member,

                Membership = latestMembership,

                LatestApplication = latestApplication,

                AttendanceCount = attendanceCount,

                ReservationCount = reservationCount,

                TotalReservations = totalReservations,

                TotalGymMinutes = totalGymMinutes,

                AverageVisitMinutes = averageVisitMinutes,

                WeeklyGymMinutes = weeklyGymMinutes,

                EstimatedCalories = estimatedCalories,

                WeeklyEstimatedCalories = weeklyEstimatedCalories,

                WeeklyLabels = weeklyLabels,

                WeeklyVisitCounts = weeklyVisitCounts,

                Workouts = workouts,

                WorkoutProfile = workoutProfile,

                WeightHistory = weightHistory,

                WorkoutCompletions = workoutCompletions,

                CompletedWorkoutDays = completedWorkoutDays,

                TotalWorkoutDays = totalWorkoutDays,

                WorkoutCompletionPercentage =
                    workoutCompletionPercentage
            };

            return View(dashboardData);
        }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Profile(
            string name,
            string surname,
            string phoneNumber,
            IFormFile? profilePicture)
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

            if (profilePicture != null &&
                profilePicture.Length > 0)
            {
                var extension =
                    Path.GetExtension(
                        profilePicture.FileName)
                        .ToLowerInvariant();

                var allowedExtensions = new[]
                {
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".webp"
                };

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "ProfilePicture",
                        "Only JPG, JPEG, PNG and WEBP images are allowed.");
                }

                if (profilePicture.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(
                        "ProfilePicture",
                        "The profile picture must not exceed 5 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(member);
            }

            member.Name = name.Trim();
            member.Surname = surname.Trim();
            member.PhoneNumber = phoneNumber.Trim();

            if (profilePicture != null &&
                profilePicture.Length > 0)
            {
                var uploadsFolder =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "profiles");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(
                        uploadsFolder);
                }

                var extension =
                    Path.GetExtension(
                        profilePicture.FileName)
                        .ToLowerInvariant();

                var uniqueFileName =
                    $"{Guid.NewGuid()}{extension}";

                var filePath =
                    Path.Combine(
                        uploadsFolder,
                        uniqueFileName);

                using (var fileStream =
                    new FileStream(
                        filePath,
                        FileMode.Create))
                {
                    profilePicture.CopyTo(
                        fileStream);
                }

                DeleteProfilePicture(
                    member.ProfilePicture);

                member.ProfilePicture =
                    $"/uploads/profiles/{uniqueFileName}";
            }

            _context.SaveChanges();

            TempData["ProfileSuccess"] =
                "Your profile has been updated successfully.";

            return RedirectToAction(nameof(Profile));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemovePhoto()
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

            if (string.IsNullOrWhiteSpace(member.ProfilePicture))
            {
                TempData["ProfileError"] =
                    "You do not currently have a profile picture.";

                return RedirectToAction(nameof(Profile));
            }

            DeleteProfilePicture(
                member.ProfilePicture);

            member.ProfilePicture = null;

            _context.SaveChanges();

            TempData["ProfileSuccess"] =
                "Your profile picture has been removed.";

            return RedirectToAction(nameof(Profile));
        }

        private void DeleteProfilePicture(
            string? profilePicture)
        {
            if (string.IsNullOrWhiteSpace(profilePicture))
            {
                return;
            }

            var normalizedPath =
                profilePicture
                    .TrimStart('/')
                    .Replace(
                        '/',
                        Path.DirectorySeparatorChar);

            var uploadsRoot =
                Path.GetFullPath(
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "profiles"));

            var filePath =
                Path.GetFullPath(
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        normalizedPath));

            if (!filePath.StartsWith(
                    uploadsRoot +
                    Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        [HttpGet]
        public IActionResult Attendance()
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var attendanceRecords = _context.Attendances
                .Where(a =>
                    a.MemberId == memberId.Value)
                .OrderByDescending(a =>
                    a.CheckInTime)
                .ToList();

            var attendance = attendanceRecords
                .Select(a =>
                {
                    var visitEndTime =
                        a.CheckOutTime ?? DateTime.Now;

                    var reservations = _context.Reservations
                        .Where(r =>
                            r.MemberID == memberId.Value &&
                            r.ReservationDate >= a.CheckInTime &&
                            r.ReservationDate <= visitEndTime)
                        .ToList();

                    var equipmentCount =
                        reservations
                            .Select(r => r.EquipmentID)
                            .Distinct()
                            .Count();

                    var equipmentRewardPoints = 0;

                    if (a.CheckOutTime.HasValue &&
                        equipmentCount > 0)
                    {
                        equipmentRewardPoints =
                            _rewardService.GetEquipmentReward(
                                equipmentCount);
                    }

                    var checkInRewardPoints =
                        _context.RewardPoints
                            .Where(r =>
                                r.MemberId == memberId.Value &&
                                r.EarnedAt >= a.CheckInTime &&
                                r.EarnedAt <= visitEndTime &&
                                r.Reason == "Gym check-in")
                            .Sum(r => (int?)r.Points) ?? 0;

                    var duration =
                        a.CheckOutTime.HasValue
                            ? a.CheckOutTime.Value -
                              a.CheckInTime
                            : (TimeSpan?)null;

                    return new AttendanceViewModel
                    {
                        AttendanceId =
                            a.AttendanceId,

                        CheckInTime =
                            a.CheckInTime,

                        CheckOutTime =
                            a.CheckOutTime,

                        Duration =
                            duration,

                        EquipmentCount =
                            equipmentCount,

                        EquipmentRewardPoints =
                            equipmentRewardPoints,

                        CheckInRewardPoints =
                            checkInRewardPoints,

                        TotalRewardPoints =
                            checkInRewardPoints +
                            equipmentRewardPoints
                    };
                })
                .ToList();

            return View(attendance);

}


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
                .Include(p => p.EquipmentPenalty)
                .Where(p =>
                    p.MemberId == memberId.Value)
                .OrderByDescending(p =>
                    p.PaymentDate)
                .ToList();

            return View(payments);
        }

        [HttpGet]
        public IActionResult Announcements()
        {
            var announcements = _context.Announcements
                .OrderByDescending(a =>
                    a.DatePosted)
                .ToList();

            return View(announcements);
        }

        [HttpGet]
        public IActionResult Membership()
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

            var latestMembership = _context.Memberships
                .Where(m =>
                    m.MemberId == memberId.Value)
                .OrderByDescending(m =>
                    m.MembershipId)
                .FirstOrDefault();

            var latestApplication = _context.MembershipApplications
                .Where(a =>
                    a.MemberId == memberId.Value)
                .OrderByDescending(a =>
                    a.MembershipApplicationId)
                .FirstOrDefault();

            ViewBag.MemberRole = member.Role;

            ViewBag.SemesterPrice =
                _membershipPricingService.GetBasePrice(
                    member.Role,
                    "Semester1");

            ViewBag.AnnualPrice =
                _membershipPricingService.GetBasePrice(
                    member.Role,
                    "Annual");

            var isFirstTimeMember =
                !_context.Memberships.Any(m =>
                    m.MemberId == memberId.Value &&
                    (m.Status == "Active" ||
                     m.PaymentStatus == "Completed"));

            ViewBag.FirstTimeDiscount =
                _membershipPricingService.GetDiscountPercentage(
                    member.Role,
                    isFirstTimeMember);

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
                ViewBag.PendingApplication =
                    latestApplication;

                return View(null);
            }

            if (latestApplication != null &&
                latestApplication.Status == "Approved")
            {
                ViewBag.PendingApplication =
                    latestApplication;

                return View(latestMembership);
            }

            if (latestApplication != null &&
                latestApplication.Status == "Rejected")
            {
                ViewBag.PendingApplication =
                    latestApplication;

                return View(null);
            }

            ViewBag.PendingApplication = null;

            return View(null);
        }

        [HttpGet]
        public IActionResult Create()
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

            var latestMembership = _context.Memberships
                .Where(m =>
                    m.MemberId == memberId.Value)
                .OrderByDescending(m =>
                    m.MembershipId)
                .FirstOrDefault();

            if (latestMembership != null &&
                latestMembership.Status == "Active" &&
                latestMembership.EndDate.HasValue &&
                latestMembership.EndDate.Value.Date >=
                DateTime.Today)
            {
                TempData["MembershipError"] =
                    "You already have an active membership.";

                return RedirectToAction(
                    nameof(Membership));
            }

            if (latestMembership != null &&
                latestMembership.Status ==
                "WaitingForPayment")
            {
                TempData["MembershipError"] =
                    "Your membership has been approved and is waiting for payment.";

                return RedirectToAction(
                    nameof(Membership));
            }

            var pendingApplication =
                _context.MembershipApplications
                    .Where(a =>
                        a.MemberId == memberId.Value &&
                        a.Status == "Pending")
                    .OrderByDescending(a =>
                        a.MembershipApplicationId)
                    .FirstOrDefault();

            if (pendingApplication != null)
            {
                TempData["MembershipError"] =
                    "You already have a pending membership application.";

                return RedirectToAction(
                    nameof(Membership));
            }

            bool isFirstTimeMember =
                !_context.Memberships.Any(m =>
                    m.MemberId == memberId.Value &&
                    (m.Status == "Active" ||
                     m.PaymentStatus == "Completed"));

            ViewBag.IsFirstTimeMember =
                isFirstTimeMember;

            ViewBag.MemberRole =
                member.Role;

            ViewBag.Semester1Available =
                _membershipPricingService.IsMembershipPeriodAvailable(
                    "Semester1",
                    DateTime.Today);

            ViewBag.Semester2Available =
                _membershipPricingService.IsMembershipPeriodAvailable(
                    "Semester2",
                    DateTime.Today);

            ViewBag.AnnualAvailable =
                _membershipPricingService.IsMembershipPeriodAvailable(
                    "Annual",
                    DateTime.Today);

            var membershipPage =
                new MembershipPage
                {
                    Name = member.Name,
                    Surname = member.Surname,
                    Email = member.Email,
                    StudentNo = member.StudentNumber
                };

            return View(membershipPage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            MembershipPage membershipPage)
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

            var activeMembership =
                _context.Memberships
                    .FirstOrDefault(m =>
                        m.MemberId == memberId.Value &&
                        m.Status == "Active" &&
                        m.EndDate.HasValue &&
                        m.EndDate.Value.Date >=
                        DateTime.Today);

            if (activeMembership != null)
            {
                TempData["MembershipError"] =
                    "You already have an active membership.";

                return RedirectToAction(
                    nameof(Membership));
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

                return RedirectToAction(
                    nameof(Membership));
            }

            var validPeriods = new[]
            {
                "Annual",
                "Semester1",
                "Semester2"
            };

            if (!validPeriods.Contains(
                membershipPage.MembershipPeriod))
            {
                ModelState.AddModelError(
                    "MembershipPeriod",
                    "Please select a valid membership period.");
            }

            if (validPeriods.Contains(
                membershipPage.MembershipPeriod))
            {
                if (!_membershipPricingService.IsMembershipPeriodAvailable(
                        membershipPage.MembershipPeriod!,
                        DateTime.Today))
                {
                    ModelState.AddModelError(
                        "MembershipPeriod",
                        "The selected membership period is not currently available.");
                }
            }

            if (string.IsNullOrWhiteSpace(
                membershipPage.PaymentMethod))
            {
                ModelState.AddModelError(
                    "PaymentMethod",
                    "Please select a payment method.");
            }

            bool isFirstTimeMember =
                !_context.Memberships.Any(m =>
                    m.MemberId == memberId.Value &&
                    (m.Status == "Active" ||
                     m.PaymentStatus == "Completed"));

            decimal basePrice = 0m;
            decimal discountPercentage = 0m;
            decimal price = 0m;

            if (validPeriods.Contains(
                membershipPage.MembershipPeriod))
            {
                try
                {
                    basePrice =
                        _membershipPricingService.GetBasePrice(
                            member.Role,
                            membershipPage.MembershipPeriod!);

                    discountPercentage =
                        _membershipPricingService.GetDiscountPercentage(
                            member.Role,
                            isFirstTimeMember);

                    price =
                        _membershipPricingService.CalculatePrice(
                            member.Role,
                            membershipPage.MembershipPeriod!,
                            isFirstTimeMember);
                }
                catch (ArgumentException)
                {
                    ModelState.AddModelError(
                        "MembershipPeriod",
                        "The selected membership option is invalid.");
                }
            }

            if (membershipPage.VerificationDocument == null ||
                membershipPage.VerificationDocument.Length == 0)
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

                ViewBag.MemberRole =
                    member.Role;

                ViewBag.Semester1Available =
                    _membershipPricingService.IsMembershipPeriodAvailable(
                        "Semester1",
                        DateTime.Today);

                ViewBag.Semester2Available =
                    _membershipPricingService.IsMembershipPeriodAvailable(
                        "Semester2",
                        DateTime.Today);

                ViewBag.AnnualAvailable =
                    _membershipPricingService.IsMembershipPeriodAvailable(
                        "Annual",
                        DateTime.Today);

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

                    BasePrice =
                        basePrice,

                    DiscountPercentage =
                        discountPercentage,

                    FirstTimeMember =
                        isFirstTimeMember,

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
                    $"Your {discountPercentage:0}% first-time member discount has been applied. " +
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
                        m.EndDate.Value.Date >=
                        DateTime.Today)
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
                !string.IsNullOrWhiteSpace(
                    member.StudentNumber))
            {
                string barcodeValue =
                    member.StudentNumber;

                var writer =
                    new BarcodeWriterPixelData
                    {
                        Format =
                            BarcodeFormat.CODE_128,

                        Options =
                            new EncodingOptions
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
                    m.EndDate.Value.Date >=
                    DateTime.Today)
                .OrderByDescending(m =>
                    m.MembershipId)
                .FirstOrDefault();

            var currentAttendance =
                _context.Attendances
                    .Where(a =>
                        a.MemberId == memberId.Value &&
                        a.CheckOutTime == null)
                    .OrderByDescending(a =>
                        a.CheckInTime)
                    .FirstOrDefault();

            var viewModel =
                new CheckInViewModel
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

        [HttpGet]
        public IActionResult CheckInPage()
        {
            return RedirectToAction(
                nameof(CheckIn));
        }

        [HttpGet]
        public IActionResult CheckInResult()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CheckOut()
        {
            var memberId = GetMemberId();

        if (memberId == null)
                    {
                        return RedirectToAction("Login", "Account");
                    }

                    var attendance = _context.Attendances
                        .FirstOrDefault(a =>
                            a.MemberId == memberId.Value &&
                            !a.CheckOutTime.HasValue);

                    if (attendance == null)
                    {
                        TempData["Error"] =
                            "No active gym attendance found.";

                        return RedirectToAction("Attendance");
                    }

                    var checkoutTime = DateTime.Now;

                    var equipmentCount = _context.Reservations
                        .Where(r =>
                            r.MemberID == memberId.Value &&
                            r.ReservationDate >= attendance.CheckInTime &&
                            r.ReservationDate <= checkoutTime)
                        .Select(r => r.EquipmentID)
                        .Distinct()
                        .Count();

                    var equipmentRewardPoints =
                        equipmentCount > 0
                            ? _rewardService.GetEquipmentReward(
                                equipmentCount)
                            : 0;

                    attendance.CheckOutTime =
                        checkoutTime;

                    if (equipmentRewardPoints > 0)
                    {
                        var rewardReason =
                            $"Equipment reward for attendance #{attendance.AttendanceId}";

                        var rewardAlreadyExists =
                            _context.RewardPoints
                                .Any(r =>
                                    r.MemberId == memberId.Value &&
                                    r.Reason == rewardReason);

                        if (!rewardAlreadyExists)
                        {
                            _context.RewardPoints.Add(
                                new RewardPoint
                                {
                                    MemberId =
                                        memberId.Value,

                                    Points =
                                        equipmentRewardPoints,

                                    Reason =
                                        rewardReason,

                                    EarnedAt =
                                        checkoutTime
                                });
                        }
                    }

                    _context.SaveChanges();

                    if (equipmentRewardPoints > 0)
                    {
                        TempData["Success"] =
                            $"You have successfully checked out. You earned +{equipmentRewardPoints} equipment reward points.";
                    }
                    else
                    {
                        TempData["Success"] =
                            "You have successfully checked out of the gym.";
                    }

                    return RedirectToAction("Attendance");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyQrCheckIn(
    string qrData)
        {
            if (string.IsNullOrWhiteSpace(qrData))
            {
                TempData["CheckInError"] =
                    "No gym QR code was detected.";

                return RedirectToAction(
                    nameof(CheckIn));
            }

            qrData =
                qrData.Trim();

            if (!string.Equals(
                qrData,
                "DUTGYM_CHECKIN",
                StringComparison.OrdinalIgnoreCase))
            {
                TempData["CheckInError"] =
                    "Invalid DUT Campus FIT Gym QR code.";

                return RedirectToAction(
                    nameof(CheckIn));
            }

            var memberId =
                GetMemberId();

            if (memberId == null)
            {
                TempData["CheckInError"] =
                    "Your account could not be identified. Please log in again.";

                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var member =
                _context.Members
                    .FirstOrDefault(m =>
                        m.MemberId ==
                        memberId.Value);

            if (member == null)
            {
                TempData["CheckInError"] =
                    "Your member account could not be found.";

                return RedirectToAction(
                    nameof(CheckIn));
            }

            var membership =
                _context.Memberships
                    .Where(m =>
                        m.MemberId ==
                            memberId.Value &&
                        m.Status ==
                            "Active")
                    .OrderByDescending(m =>
                        m.MembershipId)
                    .FirstOrDefault();

            if (membership == null)
            {
                TempData["CheckInError"] =
                    "You do not have an active gym membership.";

                return RedirectToAction(
                    nameof(CheckIn));
            }

            if (membership.EndDate.HasValue &&
                membership.EndDate.Value.Date <
                DateTime.Today)
            {
                TempData["CheckInError"] =
                    "Your gym membership has expired.";

                return RedirectToAction(
                    nameof(CheckIn));
            }

            var existingAttendance =
                _context.Attendances
                    .FirstOrDefault(a =>
                        a.MemberId ==
                            memberId.Value &&
                        a.CheckOutTime ==
                            null);

            if (existingAttendance != null)
            {
                TempData["CheckInError"] =
                    "You are already checked in.";

                return RedirectToAction(
                    nameof(CheckIn));
            }

            var checkInTime =
                DateTime.Now;

            var checkInReward =
                _rewardService.GetCheckInReward();

            var attendance =
                new Attendance
                {
                    MemberId =
                        memberId.Value,

                    CheckInTime =
                        checkInTime,

                    CheckOutTime =
                        null
                };

            var reward =
                new RewardPoint
                {
                    MemberId =
                        memberId.Value,

                    Points =
                        checkInReward,

                    Reason =
                        "Gym check-in",

                    EarnedAt =
                        checkInTime
                };

            _context.Attendances.Add(
                attendance);

            _context.RewardPoints.Add(
                reward);

            _context.SaveChanges();

            var savedReward = _context.RewardPoints
                .Any(r =>
                    r.MemberId == memberId.Value &&
                    r.Reason == "Gym check-in" &&
                    r.EarnedAt == checkInTime);

            TempData["CheckInSuccess"] =
                savedReward
                    ? $"Access granted — Welcome {member.Name}! You earned +{checkInReward} reward point."
                    : "Access granted, but the check-in reward could not be saved.";

            return RedirectToAction(
                nameof(CheckIn));
        }

        [HttpGet]
        public IActionResult Equipment()
        {
            var equipment =
                _context.Equipment
                    .Where(e => !e.IsRetired)
                    .OrderBy(e => e.EquipmentName)
                    .ToList();

            return View(equipment);
        }

        [HttpGet]
        public IActionResult Reservations()
        {
            var memberId =
                GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var now =
                DateTime.Now;

            var expiredReservations =
                _context.Reservations
                    .Where(r =>
                        r.MemberID ==
                            memberId.Value &&
                        r.Status ==
                            "Reserved" &&
                        r.EndTime <=
                            now)
                    .ToList();

            foreach (var expired
                in expiredReservations)
            {
                expired.Status =
                    "Expired";

                var equipment =
                    _context.Equipment
                        .FirstOrDefault(e =>
                            e.EquipmentID ==
                            expired.EquipmentID);

                if (equipment != null &&
                     !equipment.IsRetired)
                {
                    equipment.IsAvailable = true;
                }
            }

            if (expiredReservations.Any())
            {
                _context.SaveChanges();
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Unreserve(
            int reservationId)
        {
            var memberId =
                GetMemberId();

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

            if (equipment != null &&
                    !equipment.IsRetired)
            {
                equipment.IsAvailable = true;
            }

            _context.SaveChanges();

            TempData["EquipmentSuccess"] =
                "Equipment reservation cancelled successfully.";

            return RedirectToAction(
                nameof(Reservations));
        }

        [HttpGet]
        public IActionResult RequestTrainer(
            string? category)
        {
            var memberId =
                GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var member =
                _context.Members
                    .FirstOrDefault(m =>
                        m.MemberId ==
                        memberId.Value);

            if (member == null)
            {
                return NotFound();
            }

            var categories =
                new[]
                {
                    "All Categories",
                    "General Fitness",
                    "Strength Training",
                    "Cardio & Endurance",
                    "Weight Management",
                    "Sports Training",
                    "Functional Training"
                };

            var trainersQuery =
                _context.Trainers
                    .AsQueryable();

            if (!string.IsNullOrWhiteSpace(category) &&
                category != "All Categories")
            {
                trainersQuery =
                    trainersQuery
                        .Where(t =>
                            t.Category ==
                            category);
            }

            var trainers =
                trainersQuery
                    .OrderBy(t =>
                        t.TrainerName)
                    .ToList();

            ViewBag.Trainers =
                trainers;

            ViewBag.Categories =
                categories;

            ViewBag.SelectedCategory =
                category ??
                "All Categories";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestTrainer(
    int trainerId,
    string requestMessage)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var member = await _context.Members
                .FirstOrDefaultAsync(m =>
                    m.MemberId == memberId.Value);

            if (member == null)
            {
                return NotFound();
            }

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t =>
                    t.TrainerId == trainerId);

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

            requestMessage = requestMessage.Trim();

            if (requestMessage.Length > 500)
            {
                TempData["TrainerRequestError"] =
                    "Your request message cannot exceed 500 characters.";

                return RedirectToAction(
                    nameof(RequestTrainer));
            }

            var activeRequest = await _context.TrainerRequests
                .AnyAsync(r =>
                    r.StudentId == memberId.Value &&
                    (
                        r.Status == "Pending" ||
                        r.Status == "Accepted"
                    ));
            var currentMonth =
    DateTime.Now.Month;

            var currentYear =
                DateTime.Now.Year;

            var trainerMonthlyAssignments =
                await _context.TrainerRequests
                    .CountAsync(r =>
                        r.TrainerId == trainerId &&
                        (r.Status == "Accepted" ||
                         r.Status == "Completed") &&
                        r.ResponseDate.HasValue &&
                        r.ResponseDate.Value.Month == currentMonth &&
                        r.ResponseDate.Value.Year == currentYear);

            if (trainerMonthlyAssignments >= 2)
            {
                TempData["TrainerRequestError"] =
                    "This trainer has reached the maximum of 2 student assignments for this month. Please choose another trainer.";

                return RedirectToAction(
                    nameof(RequestTrainer));
            }

            if (activeRequest)
            {
                TempData["TrainerRequestError"] =
                    "You already have a pending or active trainer request.";

                return RedirectToAction(
                    nameof(MyTrainerRequests));
            }

            var trainerRequest = new TrainerRequest
            {
                StudentId = memberId.Value,
                TrainerId = trainerId,
                RequestMessage = requestMessage,
                Status = "Pending",
                RequestDate = DateTime.Now
            };

            _context.TrainerRequests.Add(trainerRequest);

            await _context.SaveChangesAsync();

            try
            {
                await SendTrainerRequestEmail(
                    member,
                    trainer,
                    trainerRequest);
            }
            catch
            {
            }

            TempData["TrainerRequestSuccess"] =
                $"Your request has been sent to {trainer.TrainerName}.";

            return RedirectToAction(
                nameof(MyTrainerRequests));
        }

        private async Task SendTrainerRequestEmail(
    Member member,
    Trainer trainer,
    TrainerRequest trainerRequest)
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
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(trainer.Email))
            {
                return;
            }

            if (!int.TryParse(
                portValue,
                out int port))
            {
                port = 587;
            }

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    "DUT Campus FIT Gym",
                    senderEmail));

            message.To.Add(
                new MailboxAddress(
                    trainer.TrainerName,
                    trainer.Email));

            message.Subject =
                "New Trainer Request - DUT Campus FIT Gym";

            var builder = new BodyBuilder();

            builder.TextBody =
                $"Hello {trainer.TrainerName},\n\n" +
                "You have received a new trainer request through DUT Campus FIT Gym.\n\n" +
                $"Member: {member.Name} {member.Surname}\n" +
                $"Student Number: {member.StudentNumber}\n" +
                $"Email: {member.Email}\n\n" +
                "Request Message:\n" +
                $"{trainerRequest.RequestMessage}\n\n" +
                "Please log into DUT Campus FIT Gym to review and respond to this request.\n\n" +
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
        public async Task<IActionResult> PrivateTrainer()
        {
            var memberId = GetMemberId();

            if (memberId == null)
                return RedirectToAction("Login", "Account");

            var activeSubscription = await _context.PrivateTrainerSubscriptions
                .Include(s => s.Trainer)
                .FirstOrDefaultAsync(s =>
                    s.MemberId == memberId.Value &&
                    s.Status == "Active" &&
                    s.EndDate >= DateTime.Now);

            var trainers = await _context.Trainers
                .OrderBy(t => t.TrainerName)
                .ToListAsync();

            var trainerAvailability = new List<object>();

            foreach (var trainer in trainers)
            {
                var privateMemberCount = await _context.PrivateTrainerSubscriptions
                    .CountAsync(s =>
                        s.TrainerId == trainer.TrainerId &&
                        s.Status == "Active" &&
                        s.EndDate >= DateTime.Now);

                trainerAvailability.Add(new
                {
                    Trainer = trainer,
                    PrivateMemberCount = privateMemberCount,
                    IsFull = privateMemberCount >= 2
                });
            }

            ViewBag.TrainerAvailability = trainerAvailability;
            ViewBag.ActiveSubscription = activeSubscription;
            ViewBag.PrivateTrainerFee = 145m;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SelectPrivateTrainer(int trainerId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
                return RedirectToAction("Login", "Account");

            var trainer = await _context.Trainers
                .FirstOrDefaultAsync(t => t.TrainerId == trainerId);

            if (trainer == null)
            {
                TempData["PrivateTrainerError"] = "The selected trainer could not be found.";
                return RedirectToAction(nameof(PrivateTrainer));
            }

            var existingSubscription = await _context.PrivateTrainerSubscriptions
                .AnyAsync(s =>
                    s.MemberId == memberId.Value &&
                    s.Status == "Active" &&
                    s.EndDate >= DateTime.Now);

            if (existingSubscription)
            {
                TempData["PrivateTrainerError"] = "You already have an active private trainer.";
                return RedirectToAction(nameof(PrivateTrainer));
            }

            var trainerMemberCount = await _context.PrivateTrainerSubscriptions
                .CountAsync(s =>
                    s.TrainerId == trainerId &&
                    s.Status == "Active" &&
                    s.EndDate >= DateTime.Now);

            if (trainerMemberCount >= 2)
            {
                TempData["PrivateTrainerError"] = "This trainer already has the maximum of 2 private members.";
                return RedirectToAction(nameof(PrivateTrainer));
            }

            var pendingSubscription = await _context.PrivateTrainerSubscriptions
                .FirstOrDefaultAsync(s =>
                    s.MemberId == memberId.Value &&
                    s.Status == "PendingPayment");

            if (pendingSubscription != null)
            {
                pendingSubscription.TrainerId = trainerId;
                pendingSubscription.Amount = 145m;
                pendingSubscription.StartDate = DateTime.Now;
                pendingSubscription.EndDate = DateTime.Now.AddMonths(1).AddDays(-1);
            }
            else
            {
                pendingSubscription = new PrivateTrainerSubscription
                {
                    MemberId = memberId.Value,
                    TrainerId = trainerId,
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddMonths(1).AddDays(-1),
                    Amount = 145m,
                    Status = "PendingPayment",
                    CreatedDate = DateTime.Now
                };

                _context.PrivateTrainerSubscriptions.Add(pendingSubscription);
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(PrivateTrainerPayment), new
            {
                id = pendingSubscription.PrivateTrainerSubscriptionId
            });
        }

        [HttpGet]
        public async Task<IActionResult> PrivateTrainerPayment(int id)
        {
            var memberId = GetMemberId();

            if (memberId == null)
                return RedirectToAction("Login", "Account");

            var subscription = await _context.PrivateTrainerSubscriptions
                .Include(s => s.Trainer)
                .FirstOrDefaultAsync(s =>
                    s.PrivateTrainerSubscriptionId == id &&
                    s.MemberId == memberId.Value &&
                    s.Status == "PendingPayment");

            if (subscription == null)
            {
                TempData["PrivateTrainerError"] = "The private trainer subscription could not be found.";
                return RedirectToAction(nameof(PrivateTrainer));
            }

            ViewBag.PrivateTrainerFee = 145m;

            return View(subscription);
        }
        private async Task SendTrainerBookingEmail(
    Member member,
    Trainer trainer,
    TrainerBooking booking)
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
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(trainer.Email))
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
                    trainer.TrainerName,
                    trainer.Email));

            message.Subject =
                "Trainer Session Booked - DUT Campus FIT Gym";

            var builder =
                new BodyBuilder();

            builder.TextBody =
                $"Hello {trainer.TrainerName},\n\n" +
                "A student has booked a training session with you through DUT Campus FIT Gym.\n\n" +
                $"Student: {member.Name} {member.Surname}\n" +
                $"Student Number: {member.StudentNumber}\n" +
                $"Email: {member.Email}\n\n" +
                $"Session Date: {booking.StartTime:dd MMM yyyy}\n" +
                $"Start Time: {booking.StartTime:HH:mm}\n" +
                $"End Time: {booking.EndTime:HH:mm}\n\n" +
                "Please log into DUT Campus FIT Gym to view your upcoming training sessions.\n\n" +
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

        private async Task SendTrainerAcceptedEmail(
    Member member,
    Trainer trainer)
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
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(member.Email))
            {
                return;
            }

            if (!int.TryParse(
                portValue,
                out int port))
            {
                port = 587;
            }

            var message = new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    "DUT Campus FIT Gym",
                    senderEmail));

            message.To.Add(
                new MailboxAddress(
                    $"{member.Name} {member.Surname}",
                    member.Email));

            message.Subject =
                "Trainer Request Accepted - DUT Campus FIT Gym";

            var builder = new BodyBuilder();

            builder.TextBody =
                $"Hello {member.Name},\n\n" +
                $"Your trainer request has been accepted by {trainer.TrainerName}.\n\n" +
                "You can now log into DUT Campus FIT Gym and book a training session with your trainer.\n\n" +
                "You may book a session for a future date and time that suits you and your trainer.\n\n" +
                $"Trainer: {trainer.TrainerName}\n" +
                $"Category: {trainer.Category}\n\n" +
                "Please log into DUT Campus FIT Gym to schedule your session.\n\n" +
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
        public IActionResult MyTrainerRequests()
        {
            var memberId =
                GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var requests =
                _context.TrainerRequests
                    .Include(r =>
                        r.Trainer)
                    .Where(r =>
                        r.StudentId ==
                        memberId.Value)
                    .OrderByDescending(r =>
                        r.RequestDate)
                    .ToList();

            return View(requests);
        }

        [HttpGet]
        public async Task<IActionResult> BookTrainerSession(int requestId)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var request = await _context.TrainerRequests
                .Include(r => r.Trainer)
                .FirstOrDefaultAsync(r =>
                    r.TrainerRequestId == requestId &&
                    r.StudentId == memberId.Value &&
                    r.Status == "Accepted");

            if (request == null)
            {
                TempData["TrainerRequestError"] =
                    "This trainer request is not available for booking.";

                return RedirectToAction(
                    nameof(MyTrainerRequests));
            }

            var existingBooking = await _context.TrainerBookings
                .AnyAsync(b =>
                    b.TrainerRequestId == requestId &&
                    (
                        b.Status == "Booked" ||
                        b.Status == "PendingPayment"
                    ));

            if (existingBooking)
            {
                TempData["TrainerRequestError"] =
                    "You already have a booked or pending-payment session for this trainer request.";

                return RedirectToAction(
                    nameof(MyTrainerRequests));
            }

            ViewBag.TrainerRequest = request;
            ViewBag.TrainerSessionFee = 50m;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookTrainerSession(
            int requestId,
            DateTime sessionDate,
            TimeSpan startTime)
        {
            var memberId = GetMemberId();

            if (memberId == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var request = await _context.TrainerRequests
                .Include(r => r.Trainer)
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r =>
                    r.TrainerRequestId == requestId &&
                    r.StudentId == memberId.Value &&
                    r.Status == "Accepted");

            if (request == null)
            {
                TempData["TrainerRequestError"] =
                    "This trainer request is not available for booking.";

                return RedirectToAction(
                    nameof(MyTrainerRequests));
            }

            var startDateTime =
                sessionDate.Date.Add(startTime);

            var endDateTime =
                startDateTime.AddHours(1);

            if (startDateTime <= DateTime.Now)
            {
                TempData["TrainerRequestError"] =
                    "Please select a future date and time.";

                return RedirectToAction(
                    nameof(BookTrainerSession),
                    new { requestId });
            }

            var existingBooking = await _context.TrainerBookings
                .AnyAsync(b =>
                    b.TrainerRequestId == requestId &&
                    (
                        b.Status == "Booked" ||
                        b.Status == "PendingPayment"
                    ));

            if (existingBooking)
            {
                TempData["TrainerRequestError"] =
                    "You already have a booked or pending-payment session for this trainer request.";

                return RedirectToAction(
                    nameof(MyTrainerRequests));
            }

            var trainerConflict = await _context.TrainerBookings
                .AnyAsync(b =>
                    b.TrainerId == request.TrainerId &&
                    (
                        b.Status == "Booked" ||
                        b.Status == "PendingPayment"
                    ) &&
                    startDateTime < b.EndTime &&
                    endDateTime > b.StartTime);

            if (trainerConflict)
            {
                TempData["TrainerRequestError"] =
                    "The trainer is already booked or has a pending payment during that time. Please choose another time.";

                return RedirectToAction(
                    nameof(BookTrainerSession),
                    new { requestId });
            }

            var studentConflict = await _context.TrainerBookings
                .AnyAsync(b =>
                    b.StudentId == memberId.Value &&
                    (
                        b.Status == "Booked" ||
                        b.Status == "PendingPayment"
                    ) &&
                    startDateTime < b.EndTime &&
                    endDateTime > b.StartTime);

            if (studentConflict)
            {
                TempData["TrainerRequestError"] =
                    "You already have another trainer session during that time.";

                return RedirectToAction(
                    nameof(BookTrainerSession),
                    new { requestId });
            }

            var dayStart =
                startDateTime.Date;

            var dayEnd =
                dayStart.AddDays(1);

            var dailySessionCount =
                await _context.TrainerBookings
                    .CountAsync(b =>
                        b.TrainerId == request.TrainerId &&
                        (
                            b.Status == "Booked" ||
                            b.Status == "PendingPayment"
                        ) &&
                        b.StartTime >= dayStart &&
                        b.StartTime < dayEnd);

            if (dailySessionCount >= 3)
            {
                TempData["TrainerRequestError"] =
                    "This trainer has already reached the maximum of 3 training sessions for this day. Please choose another date.";

                return RedirectToAction(
                    nameof(BookTrainerSession),
                    new { requestId });
            }

            var booking = new TrainerBooking
            {
                TrainerRequestId =
                    request.TrainerRequestId,

                StudentId =
                    request.StudentId,

                TrainerId =
                    request.TrainerId,

                StartTime =
                    startDateTime,

                EndTime =
                    endDateTime,

                Status =
                    "PendingPayment",

                CreatedDate =
                    DateTime.Now
            };

            _context.TrainerBookings.Add(booking);

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "PayTrainerBooking",
                "Banking",
                new { bookingId = booking.TrainerBookingId });
        }

        [HttpGet]
        public IActionResult Payment()
        {
            var memberIdClaim =
                User.FindFirst(
                    ClaimTypes.NameIdentifier);

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

            var payments =
                _context.Payments
                    .Include(p =>
                        p.Membership)
                    .Include(p =>
                        p.EquipmentPenalty)
                    .Where(p =>
                        p.MemberId ==
                        memberId)
                    .OrderByDescending(p =>
                        p.PaymentDate)
                    .ToList();

            var outstandingPenalty =
                _context.EquipmentPenalties
                    .Where(p =>
                        p.MemberId == memberId &&
                        p.Status == "Outstanding")
                    .OrderBy(p =>
                        p.PenaltyDate)
                    .FirstOrDefault();

            ViewBag.OutstandingPenalty =
                outstandingPenalty;

            return View(
                "Payment",
                payments);
        }
    }
}