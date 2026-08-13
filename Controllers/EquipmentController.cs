using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DUT_Campus_FIT_Gym.ViewModels;

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
            var equipment = _context.Equipment.ToList();

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
            var memberId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
            );

            var equipment = _context.Equipment.Find(id);

            if (equipment == null)
            {
                return NotFound();
            }

            if (!equipment.IsAvailable)
            {
                return RedirectToAction(nameof(Index));
            }

            var reservation = new Reservation
            {
                MemberID = memberId,
                EquipmentID = equipment.EquipmentID,
                ReservationDate = DateTime.Now,
                Status = "Reserved"
            };

            equipment.IsAvailable = false;

            _context.Reservations.Add(reservation);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult MyReservations()
        {
            var memberId = int.Parse(
                User.FindFirstValue(ClaimTypes.NameIdentifier)
            );

            var reservations = _context.Reservations
                .Where(r => r.MemberID == memberId)
                .Join(
                    _context.Equipment,
                    reservation => reservation.EquipmentID,
                    equipment => equipment.EquipmentID,
                    (reservation, equipment) => new ReservationViewModel
                    {
                        ReservationID = reservation.ReservationID,
                        EquipmentName = equipment.EquipmentName,
                        ReservationDate = reservation.ReservationDate,
                        Status = reservation.Status
                    }
                )
                .ToList();

            return View("Reserve", reservations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Unreserve(int id)
        {
            var memberId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var reservation = _context.Reservations
                .FirstOrDefault(r => r.ReservationID == id && r.MemberID == memberId);

            if (reservation == null)
            {
                return NotFound();
            }

            var equipment = _context.Equipment
                .FirstOrDefault(e => e.EquipmentID == reservation.EquipmentID);

            if (equipment != null)
            {
                equipment.IsAvailable = true;
            }

            _context.Reservations.Remove(reservation);
            _context.SaveChanges();

            return RedirectToAction(nameof(MyReservations));
        }

    }
}