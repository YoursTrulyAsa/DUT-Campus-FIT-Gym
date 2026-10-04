using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize]
    public class EquipmentController : Controller
    {
        private readonly GymDbContext _context;

        public EquipmentController(GymDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Student,Staff")]
        public IActionResult Index()
        {
            if (!IsCheckedIn())
            {
                TempData["EquipmentError"] =
                    "Please check in to the gym before accessing equipment.";

                return RedirectToAction(
                    "Attendance",
                    "Member");
            }

            var equipment =
                _context.Equipment
                    .Where(e => !e.IsRetired)
                    .OrderBy(e => e.EquipmentName)
                    .ToList();

            var activeReservations =
                _context.Reservations
                    .Where(r =>
                        r.Status == "Reserved" &&
                        r.EndTime > DateTime.Now)
                    .ToList();

            ViewBag.ActiveReservations =
                activeReservations;

            return View(equipment);
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Trainer")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Trainer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Equipment equipment,
            IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(equipment);
            }

            equipment.IsAvailable = true;
            equipment.IsRetired = false;

            if (imageFile != null &&
                imageFile.Length > 0)
            {
                var extension =
                    Path.GetExtension(
                        imageFile.FileName)
                        .ToLowerInvariant();

                var allowedExtensions =
                    new[]
                    {
                        ".jpg",
                        ".jpeg",
                        ".png",
                        ".webp"
                    };

                if (!allowedExtensions.Contains(
                        extension))
                {
                    ModelState.AddModelError(
                        "imageFile",
                        "Only JPG, JPEG, PNG and WEBP images are allowed.");

                    return View(equipment);
                }

                var uploadsFolder =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        "uploads",
                        "equipment");

                Directory.CreateDirectory(
                    uploadsFolder);

                var fileName =
                    $"{Guid.NewGuid()}{extension}";

                var filePath =
                    Path.Combine(
                        uploadsFolder,
                        fileName);

                using (var stream =
                       new FileStream(
                           filePath,
                           FileMode.Create))
                {
                    await imageFile.CopyToAsync(
                        stream);
                }

                equipment.ImagePath =
                    $"/uploads/equipment/{fileName}";
            }

            _context.Equipment.Add(equipment);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Equipment added successfully.";

            return RedirectToAction(
                "Equipment",
                "Admin");
        }

        [HttpPost]
        [Authorize(Roles = "Student,Staff")]
        [ValidateAntiForgeryToken]
        public IActionResult Reserve(int id)
        {
            if (!IsCheckedIn())
            {
                TempData["EquipmentError"] =
                    "Please check in to the gym before reserving equipment.";

                return RedirectToAction(
                    "Attendance",
                    "Member");
            }

            var memberIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim) ||
                !int.TryParse(
                    memberIdClaim,
                    out int memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var existingReservation =
                _context.Reservations
                    .FirstOrDefault(r =>
                        r.MemberID == memberId &&
                        r.Status == "Reserved" &&
                        r.EndTime > DateTime.Now);

            if (existingReservation != null)
            {
                TempData["EquipmentError"] =
                    "You already have an equipment reservation. Cancel it or wait for it to expire.";

                return RedirectToAction(
                    nameof(MyReservations));
            }

            var equipment =
                _context.Equipment
                    .FirstOrDefault(e =>
                        e.EquipmentID == id);

            if (equipment == null)
            {
                return NotFound();
            }

            if (equipment.IsRetired)
            {
                TempData["EquipmentError"] =
                    "This equipment has been retired and is no longer available for reservation.";

                return RedirectToAction(
                    nameof(Index));
            }

            var equipmentCooldown =
                _context.Reservations
                    .Where(r =>
                        r.EquipmentID == id &&
                        r.Status == "Expired")
                    .OrderByDescending(r =>
                        r.EndTime)
                    .FirstOrDefault();

            if (equipmentCooldown != null)
            {
                var cooldownEnd =
                    equipmentCooldown.EndTime
                        .AddMinutes(2);

                if (cooldownEnd >
                    DateTime.Now)
                {
                    var secondsRemaining =
                        (int)Math.Ceiling(
                            (
                                cooldownEnd -
                                DateTime.Now
                            ).TotalSeconds);

                    TempData["EquipmentError"] =
                        $"This equipment is cooling down. Please wait {secondsRemaining} seconds before reserving it.";

                    return RedirectToAction(
                        nameof(Index));
                }
            }

            if (!equipment.IsAvailable)
            {
                TempData["EquipmentError"] =
                    "This equipment is currently reserved by another student.";

                return RedirectToAction(
                    nameof(Index));
            }

            var startTime =
                DateTime.Now;

            var endTime =
                startTime.AddMinutes(1);

            var reservation =
                new Reservation
                {
                    MemberID =
                        memberId,

                    EquipmentID =
                        equipment.EquipmentID,

                    ReservationDate =
                        startTime,

                    EndTime =
                        endTime,

                    Status =
                        "Reserved",

                    NotificationDismissed =
                        false,

                    PenaltyApplied =
                        false
                };

            equipment.IsAvailable =
                false;

            _context.Reservations.Add(
                reservation);

            _context.SaveChanges();

            TempData["EquipmentSuccess"] =
                $"{equipment.EquipmentName} reserved successfully for 10 minutes for your workout";

            return RedirectToAction(
                nameof(MyReservations));
        }

        [HttpGet]
        [Authorize(Roles = "Student,Staff")]
        public IActionResult MyReservations()
        {
            var memberIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim) ||
                !int.TryParse(
                    memberIdClaim,
                    out int memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var reservations =
                _context.Reservations
                    .Where(r =>
                        r.MemberID == memberId)
                    .Join(
                        _context.Equipment,
                        reservation =>
                            reservation.EquipmentID,
                        equipment =>
                            equipment.EquipmentID,
                        (reservation, equipment) =>
                            new ReservationViewModel
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
                                    reservation.Status,

                                NotificationDismissed =
                                    reservation.NotificationDismissed
                            }
                    )
                    .OrderByDescending(
                        r => r.ReservationDate)
                    .ToList();

            return View(
                "Reserve",
                reservations);
        }

        [HttpPost]
        [Authorize(Roles = "Student,Staff")]
        [ValidateAntiForgeryToken]
        public IActionResult Unreserve(int id)
        {
            var memberIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim) ||
                !int.TryParse(
                    memberIdClaim,
                    out int memberId))
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var reservation =
                _context.Reservations
                    .FirstOrDefault(r =>
                        r.ReservationID == id &&
                        r.MemberID == memberId &&
                        r.Status == "Reserved");

            if (reservation == null)
            {
                TempData["EquipmentError"] =
                    "The reservation could not be found or has already expired.";

                return RedirectToAction(
                    nameof(MyReservations));
            }

            if (reservation.EndTime <=
                DateTime.Now)
            {
                TempData["EquipmentError"] =
                    "The reservation has already expired.";

                return RedirectToAction(
                    nameof(MyReservations));
            }

            var equipment =
                _context.Equipment
                    .FirstOrDefault(e =>
                        e.EquipmentID ==
                        reservation.EquipmentID);

            if (equipment != null &&
                !equipment.IsRetired)
            {
                equipment.IsAvailable =
                    true;
            }

            reservation.Status =
                "Cancelled";

            reservation.NotificationDismissed =
                true;

            _context.SaveChanges();

            TempData["EquipmentSuccess"] =
                "Equipment reservation cancelled successfully.";

            return RedirectToAction(
                nameof(MyReservations));
        }

        [HttpPost]
        [Authorize(Roles = "Student,Staff")]
        [ValidateAntiForgeryToken]
        public IActionResult DismissReservationNotification(
            int reservationId)
        {
            var memberIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim) ||
                !int.TryParse(
                    memberIdClaim,
                    out int memberId))
            {
                return Unauthorized();
            }

            var reservation =
                _context.Reservations
                    .FirstOrDefault(r =>
                        r.ReservationID ==
                            reservationId &&
                        r.MemberID ==
                            memberId);

            if (reservation == null)
            {
                return NotFound();
            }

            if (reservation.Status !=
                "Reserved")
            {
                return BadRequest();
            }

            if (DateTime.Now <
                reservation.EndTime)
            {
                return BadRequest();
            }

            if (reservation.NotificationDismissed)
            {
                return Ok();
            }

            if (DateTime.Now >=
                reservation.EndTime.AddMinutes(1))
            {
                return BadRequest();
            }

            reservation.Status =
                "Expired";

            reservation.NotificationDismissed =
                true;

            var equipment =
                _context.Equipment
                    .FirstOrDefault(e =>
                        e.EquipmentID ==
                        reservation.EquipmentID);

            if (equipment != null &&
                !equipment.IsRetired)
            {
                equipment.IsAvailable =
                    true;
            }

            _context.SaveChanges();

            return Ok();
        }

        [HttpPost]
        [Authorize(Roles = "Student,Staff")]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessMissedReservationNotification(
            int reservationId)
        {
            var memberIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim) ||
                !int.TryParse(
                    memberIdClaim,
                    out int memberId))
            {
                return Unauthorized();
            }

            var reservation =
                _context.Reservations
                    .FirstOrDefault(r =>
                        r.ReservationID ==
                            reservationId &&
                        r.MemberID ==
                            memberId);

            if (reservation == null)
            {
                return NotFound();
            }

            if (reservation.PenaltyApplied)
            {
                return Ok(new
                {
                    success = true,
                    alreadyProcessed = true
                });
            }

            if (DateTime.Now <
                reservation.EndTime.AddMinutes(1))
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "The reservation notification period has not ended yet."
                });
            }

            if (reservation.Status ==
                "Cancelled")
            {
                return BadRequest(new
                {
                    success = false,
                    message =
                        "Cancelled reservations cannot receive a missed reservation penalty."
                });
            }

            reservation.Status =
                "Expired";

            reservation.PenaltyApplied =
                true;

            var equipment =
                _context.Equipment
                    .FirstOrDefault(e =>
                        e.EquipmentID ==
                        reservation.EquipmentID);

            if (equipment != null &&
                !equipment.IsRetired)
            {
                equipment.IsAvailable =
                    true;
            }

            var rewardPoint =
                new RewardPoint
                {
                    MemberId =
                        memberId,

                    Points =
                        -15,

                    Reason =
                        "Missed equipment reservation",

                    EarnedAt =
                        DateTime.Now
                };

            _context.RewardPoints.Add(
                rewardPoint);

            _context.SaveChanges();

            var latestPenalty =
                _context.EquipmentPenalties
                    .Where(p =>
                        p.MemberId == memberId)
                    .OrderByDescending(
                        p => p.PenaltyDate)
                    .FirstOrDefault();

            var missedReservationPointsQuery =
                _context.RewardPoints
                    .Where(r =>
                        r.MemberId == memberId &&
                        r.Reason ==
                            "Missed equipment reservation");

            if (latestPenalty != null)
            {
                missedReservationPointsQuery =
                    missedReservationPointsQuery
                        .Where(r =>
                            r.EarnedAt >
                            latestPenalty.PenaltyDate);
            }

            var missedReservationPoints =
                missedReservationPointsQuery
                    .Select(r => r.Points)
                    .ToList()
                    .Sum();

            var penaltyCreated =
                false;

            if (missedReservationPoints <= -45)
            {
                var equipmentPenalty =
                    new EquipmentPenalty
                    {
                        MemberId =
                            memberId,

                        ReservationId =
                            reservation.ReservationID,

                        Amount =
                            50.00m,

                        Status =
                            "Outstanding",

                        PenaltyDate =
                            DateTime.Now
                    };

                _context.EquipmentPenalties.Add(
                    equipmentPenalty);

                penaltyCreated =
                    true;

                _context.SaveChanges();
            }

            return Ok(new
            {
                success = true,
                pointsDeducted = 15,
                penaltyCreated = penaltyCreated,
                penaltyAmount =
                    penaltyCreated
                        ? 50
                        : 0
            });
        }

        private bool IsCheckedIn()
        {
            var memberIdClaim =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(
                    memberIdClaim) ||
                !int.TryParse(
                    memberIdClaim,
                    out int memberId))
            {
                return false;
            }

            var now =
                DateTime.Now;

            return _context.Attendances
                .Any(a =>
                    a.MemberId == memberId &&
                    a.CheckInTime <= now &&
                    (
                        !a.CheckOutTime.HasValue ||
                        a.CheckOutTime.Value > now
                    ));
        }
    }
}