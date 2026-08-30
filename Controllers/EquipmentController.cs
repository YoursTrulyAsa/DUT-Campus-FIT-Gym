using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DUT_Campus_FIT_Gym.Controllers
{
    public class EquipmentController : Controller
    {
        private readonly GymDbContext _context;

        public EquipmentController(GymDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            ExpireOldReservations();

            var equipment = _context.Equipment
                .OrderBy(e => e.EquipmentName)
                .ToList();

            var activeReservations = _context.Reservations
                .Where(r => r.Status == "Reserved")
                .ToList();

            ViewBag.ActiveReservations = activeReservations;

            return View(equipment);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Equipment equipment)
        {
            if (ModelState.IsValid)
            {
                equipment.IsAvailable = true;

                _context.Equipment.Add(equipment);
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            }

            return View(equipment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reserve(int id)
        {
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

                return RedirectToAction(nameof(Index));
            }

            var equipment = _context.Equipment
                .FirstOrDefault(e => e.EquipmentID == id);

            if (equipment == null)
            {
                return NotFound();
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
                Status = "Reserved"
            };

            equipment.IsAvailable = false;

            _context.Reservations.Add(reservation);
            _context.SaveChanges();

            TempData["Success"] =
                $"{equipment.EquipmentName} reserved successfully for 10 minutes.";

            return RedirectToAction(nameof(MyReservations));
        }

        public IActionResult MyReservations()
        {
            ExpireOldReservations();

            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim) ||
                !int.TryParse(memberIdClaim, out int memberId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Only show ACTIVE reservations.
            // Cancelled and expired reservations are removed
            // from this page.

            var reservations = _context.Reservations
                .Where(r =>
                    r.MemberID == memberId &&
                    r.Status == "Reserved")
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

                            EndTime =
                                reservation.EndTime,

                            Status =
                                reservation.Status
                        }
                )
                .OrderByDescending(r => r.ReservationDate)
                .ToList();

            return View("Reserve", reservations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Unreserve(int id)
        {
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
                    equipment.IsAvailable = true;
                }
            }

            if (expiredReservations.Any())
            {
                _context.SaveChanges();
            }
        }
    }
}