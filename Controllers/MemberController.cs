using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

            var dashboardData = new
            {
                Member = member,
                Membership = membership,
                AttendanceCount = attendanceCount
            };

            return View(dashboardData);
        }

        public IActionResult Profile()
        {
            var memberIdCalim = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

            if(memberIdCalim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdCalim);

            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId);

            if(member == null)
            {
                return NotFound();
            }

            return View(member);
        }

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
                return NotFound("No membership found for this member.");
            }
             
            return View(membership);
        }

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

        public IActionResult Equipment()
        {
            var equipment = _context.Equipment.ToList();

            return View(equipment);
        }

        public IActionResult Reservations()
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

            return View(reservations);
        }

        public IActionResult Announcements()
        {
            var announcements = _context.Announcements
                .OrderByDescending(a => a.DatePosted)
                .ToList();

            return View(announcements);
        }

    }
}