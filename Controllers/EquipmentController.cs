using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public IActionResult Index()
        {
            if (!User.IsInRole("Student") && !User.IsInRole("Staff"))
            {
                return Forbid();
            }

            if (!IsCheckedIn())
            {
                TempData["Error"] =
                    "Please check in to the gym before accessing equipment.";

                return RedirectToAction("Attendance", "Member");
            }

            ExpireOldReservations();

            var equipment = _context.Equipment
                .OrderBy(e => e.EquipmentName)
                .ToList();

            var activeReservations = _context.Reservations
                .Where(r =>
                    r.Status == "Reserved" &&
                    r.EndTime > DateTime.Now)
                .ToList();

            ViewBag.ActiveReservations = activeReservations;

            return View(equipment);
        }

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

            if (imageFile != null && imageFile.Length > 0)
            {
                var extension =
                    Path.GetExtension(imageFile.FileName)
                        .ToLowerInvariant();

                var allowedExtensions =
                    new[] { ".jpg", ".jpeg", ".png", ".webp" };

                if (!allowedExtensions.Contains(extension))
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

                Directory.CreateDirectory(uploadsFolder);

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
                    await imageFile.CopyToAsync(stream);
                }

                equipment.ImagePath =
                    $"/uploads/equipment/{fileName}";
            }

            _context.Equipment.Add(equipment);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Equipment added successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Student,Staff")]
        [ValidateAntiForgeryToken]
        public IActionResult Reserve(int id)
        {
            if (!IsCheckedIn())
            {
                TempData["Error"] =
                    "Please check in to the gym before reserving equipment.";

                return RedirectToAction("Attendance", "Member");
            }

            ExpireOldReservations();

            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim) ||
                !int.TryParse(memberIdClaim, out int memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            var existingReservation = _context.Reservations
                .FirstOrDefault(r =>
                    r.MemberID == memberId &&
                    r.Status == "Reserved" &&
                    r.EndTime > DateTime.Now);

            if (existingReservation != null)
            {
                TempData["Error"] =
                    "You already have an equipment reservation. Cancel it or wait for it to expire.";

                return RedirectToAction(nameof(MyReservations));
            }

            var equipment = _context.Equipment
                .FirstOrDefault(e => e.EquipmentID == id);

            if (equipment == null)
            {
                return NotFound();
            }

            var equipmentCooldown = _context.Reservations
                .Where(r =>
                    r.EquipmentID == id &&
                    r.Status == "Expired")
                .OrderByDescending(r => r.EndTime)
                .FirstOrDefault();

            if (equipmentCooldown != null)
            {
                var cooldownEnd =
                    equipmentCooldown.EndTime.AddMinutes(2);

                if (cooldownEnd > DateTime.Now)
                {
                    var secondsRemaining =
                        (int)Math.Ceiling(
                            (cooldownEnd - DateTime.Now).TotalSeconds);

                    TempData["Error"] =
                        $"This equipment is cooling down. Please wait {secondsRemaining} seconds before reserving it.";

                    return RedirectToAction(nameof(Index));
                }
            }

            if (!equipment.IsAvailable)
            {
                TempData["Error"] =
                    "This equipment is currently reserved by another student.";

                return RedirectToAction(nameof(Index));
            }

            var startTime = DateTime.Now;
            var endTime = startTime.AddMinutes(10);

            var reservation = new Reservation
            {
                MemberID = memberId,
                EquipmentID = equipment.EquipmentID,
                ReservationDate = startTime,
                EndTime = endTime,
                Status = "Reserved",
                NotificationDismissed = false
            };

            equipment.IsAvailable = false;

            _context.Reservations.Add(reservation);
            _context.SaveChanges();

            TempData["Success"] =
                $"{equipment.EquipmentName} reserved successfully for 10 minutes.";

            return RedirectToAction(nameof(MyReservations));
        }

        [Authorize(Roles = "Student,Staff")]
        public IActionResult MyReservations()
        {
            if (!IsCheckedIn())
            {
                TempData["Error"] =
                    "Please check in to the gym before accessing your reservations.";

                return RedirectToAction("Attendance", "Member");
            }

            ExpireOldReservations();

            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim) ||
                !int.TryParse(memberIdClaim, out int memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            var reservations = _context.Reservations
                .Where(r =>
                    r.MemberID == memberId)
                .Join(
                    _context.Equipment,
                    reservation => reservation.EquipmentID,
                    equipment => equipment.EquipmentID,
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
                .OrderByDescending(r => r.ReservationDate)
                .ToList();

            return View("Reserve", reservations);
        }

        [HttpPost]
        [Authorize(Roles = "Student,Staff")]
        [ValidateAntiForgeryToken]
        public IActionResult Unreserve(int id)
        {
            if (!IsCheckedIn())
            {
                TempData["Error"] =
                    "Please check in to the gym before managing equipment reservations.";

                return RedirectToAction("Attendance", "Member");
            }

            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim) ||
                !int.TryParse(memberIdClaim, out int memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            var reservation = _context.Reservations
                .FirstOrDefault(r =>
                    r.ReservationID == id &&
                    r.MemberID == memberId &&
                    r.Status == "Reserved");

            if (reservation == null)
            {
                TempData["Error"] =
                    "The reservation could not be found or has already expired.";

                return RedirectToAction(nameof(MyReservations));
            }

            var equipment = _context.Equipment
                .FirstOrDefault(e =>
                    e.EquipmentID == reservation.EquipmentID);

            if (equipment != null)
            {
                equipment.IsAvailable = true;
            }

            reservation.Status = "Cancelled";

            _context.SaveChanges();

            TempData["Success"] =
                "Equipment reservation cancelled successfully.";

            return RedirectToAction(nameof(MyReservations));
        }

        [HttpPost]
        [Authorize(Roles = "Student,Staff")]
        [ValidateAntiForgeryToken]
        public IActionResult DismissReservationNotification(
            int reservationId)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim) ||
                !int.TryParse(memberIdClaim, out int memberId))
            {
                return Unauthorized();
            }

            var reservation = _context.Reservations
                .FirstOrDefault(r =>
                    r.ReservationID == reservationId &&
                    r.MemberID == memberId);

            if (reservation == null)
            {
                return NotFound();
            }

            reservation.NotificationDismissed = true;

            _context.SaveChanges();

            return Ok();
        }

        private bool IsCheckedIn()
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(memberIdClaim) ||
                !int.TryParse(memberIdClaim, out int memberId))
            {
                return false;
            }

            var now = DateTime.Now;

            return _context.Attendances
                .Any(a =>
                    a.MemberId == memberId &&
                    a.CheckInTime <= now &&
                    (
                        !a.CheckOutTime.HasValue ||
                        a.CheckOutTime.Value > now
                    ));
        }

        private void ExpireOldReservations()
        {
            var now = DateTime.Now;

            var expiredReservations = _context.Reservations
                .Where(r =>
                    r.Status == "Reserved" &&
                    r.EndTime <= now)
                .ToList();

            foreach (var reservation in expiredReservations)
            {
                reservation.Status = "Expired";

                var equipment = _context.Equipment
                    .FirstOrDefault(e =>
                        e.EquipmentID == reservation.EquipmentID);

                if (equipment != null)
                {
                    equipment.IsAvailable = false;
                }
            }

            if (expiredReservations.Any())
            {
                _context.SaveChanges();
            }

            var cooldownFinished = _context.Reservations
                .Where(r =>
                    r.Status == "Expired" &&
                    r.EndTime.AddMinutes(2) <= now)
                .ToList();

            foreach (var reservation in cooldownFinished)
            {
                var equipment = _context.Equipment
                    .FirstOrDefault(e =>
                        e.EquipmentID == reservation.EquipmentID);

                if (equipment != null)
                {
                    var newerReservationExists =
                        _context.Reservations.Any(r =>
                            r.EquipmentID ==
                                reservation.EquipmentID &&
                            r.Status == "Reserved" &&
                            r.ReservationDate >
                                reservation.EndTime);

                    if (!newerReservationExists)
                    {
                        equipment.IsAvailable = true;
                    }
                }
            }

            if (cooldownFinished.Any())
            {
                _context.SaveChanges();
            }
        }
    }
}