using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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


        // =========================================================
        // EQUIPMENT LIST
        // =========================================================

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


        // =========================================================
        // CREATE EQUIPMENT
        // =========================================================

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


        // =========================================================
        // RESERVE EQUIPMENT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reserve(int id)
        {
            ExpireOldReservations();

            var memberId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
            );


            // -----------------------------------------------------
            // CHECK IF STUDENT ALREADY HAS AN ACTIVE RESERVATION
            // -----------------------------------------------------

            var existingReservation = _context.Reservations
                .FirstOrDefault(r =>
                    r.MemberID == memberId &&
                    r.Status == "Reserved" &&
                    r.EndTime > DateTime.Now);

            if (existingReservation != null)
            {
                TempData["Error"] =
                    "You already have an equipment reservation. Cancel it or wait for it to expire before reserving another equipment.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // FIND EQUIPMENT
            // -----------------------------------------------------

            var equipment = _context.Equipment
                .FirstOrDefault(e => e.EquipmentID == id);

            if (equipment == null)
            {
                return NotFound();
            }


            // -----------------------------------------------------
            // CHECK EQUIPMENT AVAILABILITY
            // -----------------------------------------------------

            if (!equipment.IsAvailable)
            {
                TempData["Error"] =
                    "This equipment is currently reserved by another student.";

                return RedirectToAction(nameof(Index));
            }


            // -----------------------------------------------------
            // CREATE 10-MINUTE RESERVATION
            // -----------------------------------------------------

            var startTime = DateTime.Now;

            var reservation = new Reservation
            {
                MemberID = memberId,
                EquipmentID = equipment.EquipmentID,
                ReservationDate = startTime,
                EndTime = startTime.AddMinutes(10),
                Status = "Reserved"
            };

            equipment.IsAvailable = false;

            _context.Reservations.Add(reservation);

            _context.SaveChanges();

            TempData["Success"] =
                $"{equipment.EquipmentName} reserved successfully for 10 minutes.";

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // MY RESERVATIONS
        // =========================================================

        public IActionResult MyReservations()
        {
            ExpireOldReservations();

            var memberId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
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
                            ReservationID = reservation.ReservationID,
                            EquipmentName = equipment.EquipmentName,
                            ReservationDate = reservation.ReservationDate,
                            Status = reservation.Status,
                            EndTime = reservation.EndTime
                        }
                )
                .OrderByDescending(r => r.ReservationDate)
                .ToList();

            return View("Reserve", reservations);
        }


        // =========================================================
        // CANCEL / UNRESERVE
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Unreserve(int id)
        {
            var memberId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
            );

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


        // =========================================================
        // AUTOMATICALLY EXPIRE OLD RESERVATIONS
        // =========================================================

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