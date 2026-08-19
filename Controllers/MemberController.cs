using System.Security.Claims;
using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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


        // =========================
        // DASHBOARD
        // =========================

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
               .Count(r => r.MemberID == memberId && r.Status == "Reserved");

            var dashboardData = new
            {
                Member = member,
                Membership = membership,
                AttendanceCount = attendanceCount,
                ReservationCount = reservationCount
            };

            return View(dashboardData);
        }


        // =========================
        // PROFILE
        // =========================

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


        // =========================
        // MEMBERSHIP
        // =========================

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


        // =========================
        // ATTENDANCE
        // =========================

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


        // =========================
        // CREATE MEMBERSHIP - GET
        // =========================

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

            // Get the currently logged-in member
            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId);

            if (member == null)
            {
                return NotFound();
            }

            // Automatically display the user's
            // account details on the membership page
            var membershipPage = new MembershipPage
            {
                Name = member.FirstName,
                Surname = member.LastName,
                Email = member.Email,
                StudentNo = member.StaffStudentNumber
            };

            return View(membershipPage);
        }


        // =========================
        // CREATE MEMBERSHIP - POST
        // =========================

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

            // Get the member from the database
            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId);

            if (member == null)
            {
                return NotFound();
            }


            // =========================
            // CHECK IF MEMBER ALREADY
            // HAS A MEMBERSHIP
            // =========================

            var existingMembership = _context.Memberships
                .FirstOrDefault(m => m.MemberId == memberId);

            if (existingMembership != null)
            {
                ModelState.AddModelError(
                    "",
                    "You already have a membership."
                );

                // Refill the user's details
                membershipPage.Name = member.FirstName;
                membershipPage.Surname = member.LastName;
                membershipPage.Email = member.Email;
                membershipPage.StudentNo =
                    member.StaffStudentNumber;

                return View(membershipPage);
            }


            // =========================
            // VALIDATE FORM
            // =========================

            if (!ModelState.IsValid)
            {
                // Refill user details because
                // they are not entered manually
                membershipPage.Name = member.FirstName;
                membershipPage.Surname = member.LastName;
                membershipPage.Email = member.Email;
                membershipPage.StudentNo =
                    member.StaffStudentNumber;

                return View(membershipPage);
            }


            // =========================
            // CREATE MEMBERSHIP
            // =========================

            var membership = new Membership
            {
                // Connect membership to
                // currently logged-in member
                MemberId = memberId,

                // Payment plan
                MembershipType =
                    membershipPage.payments_plan.ToString(),

                // Payment method
                PaymentMethod =
                    membershipPage.Payment_Method,

                // First time membership
                FirstTimeMember =
                    membershipPage.First_Time_Member,

                // Membership starts today
                StartDate = DateTime.Now,

                // Status
                Status = "Active",

                // Price
                Price = 0
            };


            // =========================
            // SAVE MEMBERSHIP
            // =========================

            _context.Memberships.Add(membership);

            _context.SaveChanges();


            // =========================
            // REDIRECT TO MEMBERSHIP
            // =========================

            return RedirectToAction("Membership");
        }


        // =========================
        // EQUIPMENT
        // =========================

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
