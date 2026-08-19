using System.Security.Claims;
using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
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
                Name = member.FirstName,
                Surname = member.LastName,
                Email = member.Email,
                StudentNo = member.StaffStudentNumber
            };

            return View(membershipPage);
        }

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


            // Check if member already has a membership

            var existingMembership = _context.Memberships
                .FirstOrDefault(m => m.MemberId == memberId);

            if (existingMembership != null)
            {
                ModelState.AddModelError(
                    "",
                    "You already have a membership."
                );

                membershipPage.Name = member.FirstName;
                membershipPage.Surname = member.LastName;
                membershipPage.Email = member.Email;
                membershipPage.StudentNo =
                    member.StaffStudentNumber;

                return View(membershipPage);
            }


            // Validate form

            if (!ModelState.IsValid)
            {
                membershipPage.Name = member.FirstName;
                membershipPage.Surname = member.LastName;
                membershipPage.Email = member.Email;
                membershipPage.StudentNo =
                    member.StaffStudentNumber;

                return View(membershipPage);
            }


            // Determine membership price

            decimal price = membershipPage.payments_plan switch
            {
                MembershipPage.PAY.Monthly => 150m,

                MembershipPage.PAY.Quarterly => 400m,

                MembershipPage.PAY.Half_Yearly => 700m,

                MembershipPage.PAY.Annually => 1200m,

                _ => 0m
            };


            // First-time member gets 10% discount

            decimal discount = 0m;

            if (membershipPage.First_Time_Member)
            {
                discount = price * 0.10m;
            }


            decimal finalPrice = price - discount;


            // Create membership

            var membership = new Membership
            {
                MemberId = memberId,

                MembershipType =
                    membershipPage.payments_plan.ToString(),

                PaymentMethod =
                    membershipPage.Payment_Method,

                FirstTimeMember =
                    membershipPage.First_Time_Member,

                StartDate = DateTime.Now,

                Status = "Active",

                Price = finalPrice
            };


            _context.Memberships.Add(membership);

            _context.SaveChanges();


            return RedirectToAction("Membership");
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
