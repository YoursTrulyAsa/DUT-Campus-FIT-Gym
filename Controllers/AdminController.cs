using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DUT_Campus_FIT_Gym.Controllers
{
    public class AdminController : Controller
    {
        private readonly GymDbContext _context;
        private readonly PasswordHasher<Member> _passwordHasher;

        public AdminController(GymDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Member>();
        }


        // =========================================================
        // ADMIN DASHBOARD
        // =========================================================

        public IActionResult Index()
        {
            return View();
        }


        // =========================================================
        // ADD TRAINER
        // =========================================================

        [HttpGet]
        public IActionResult AddTrainer()
        {
            return View();
        }
        // ============================
        // MANAGE EQUIPMENT
        // ============================

        [HttpGet]
        public async Task<IActionResult> Equipment()
        {
            var equipment = await _context.Equipment
                .OrderBy(e => e.EquipmentName)
                .ToListAsync();

            return View(equipment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddTrainer(CreateStaffViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ==========================================
            // CHECK EMAIL
            // ==========================================

            bool emailExistsInMembers = _context.Members
                .Any(m => m.Email == model.Email);

            bool emailExistsInTrainers = _context.Trainers
                .Any(t => t.Email == model.Email);
            bool numberExists = _context.Members
                .Any(m =>
                    m.StudentNumber ==
                    model.studentnumber);

            if (emailExistsInMembers || emailExistsInTrainers)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered.");

                return View(model);
            }


            // ==========================================
            // CHECK STAFF NUMBER
            // ==========================================

            bool numberExists = _context.Members
                .Any(m =>
                    m.StaffStudentNumber ==
                    model.StaffStudentNumber);

            if (numberExists)
            {
                ModelState.AddModelError(
                    "StaffStudentNumber",
                    "This number is already registered.");

                return View(model);
            }


            // ==========================================
            // CREATE MEMBER LOGIN ACCOUNT
            // ==========================================

            var member = new Member
            {
                Name = model.FirstName,
                Surname = model.LastName,
                StudentNumber =
                    model.studentnumber,
                Email = model.Email,

                PhoneNumber = model.PhoneNumber,

                Role = "Trainer"
            };


            // Hash password

            member.PasswordHash =
                _passwordHasher.HashPassword(
                    member,
                    model.Password);


            _context.Members.Add(member);

            _context.SaveChanges();


            // ==========================================
            // CREATE TRAINER PROFILE
            // ==========================================

            var trainer = new Trainer
            {
                TrainerName =
                    $"{model.FirstName} {model.LastName}",

                Email = model.Email
            };


            _context.Trainers.Add(trainer);

            _context.SaveChanges();


            // ==========================================
            // SUCCESS
            // ==========================================

            TempData["Success"] =
                "Trainer account created successfully.";

            return RedirectToAction("Index");
        }

        public IActionResult Scanner()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyBarcodeCheckIn(string barcodeData)
        {
            if (string.IsNullOrWhiteSpace(barcodeData))
            {
                TempData["CheckInError"] =
                    "No barcode was detected.";

                return RedirectToAction("Scanner");
            }

            // Expected format:
            // DUTGYM:2

            if (!barcodeData.StartsWith("DUTGYM:"))
            {
                TempData["CheckInError"] =
                    "Invalid DUT FIT Gym barcode.";

                return RedirectToAction("Scanner");
            }

            var memberIdText =
                barcodeData.Substring("DUTGYM:".Length).Trim();

            if (!int.TryParse(memberIdText, out int memberId))
            {
                TempData["CheckInError"] =
                    "Invalid member identification.";

                return RedirectToAction("Scanner");
            }

            // Find member
            var member = _context.Members
                .FirstOrDefault(m => m.MemberId == memberId);

            if (member == null)
            {
                TempData["CheckInError"] =
                    "Member account could not be found.";

                return RedirectToAction("Scanner");
            }

            // Find latest membership
            var membership = _context.Memberships
                .Where(m => m.MemberId == memberId)
                .OrderByDescending(m => m.EndDate)
                .FirstOrDefault();

            if (membership == null)
            {
                TempData["CheckInError"] =
                    "This member does not have a gym membership.";

                return RedirectToAction("Scanner");
            }

            // Check membership expiry
            if (membership.EndDate.HasValue && membership.EndDate.Value.Date < DateTime.Today)
            {
                TempData["CheckInError"] =
                    "This member's gym membership has expired.";
                return RedirectToAction("Scanner");
            }

            // Check if already inside the gym
            var existingAttendance = _context.Attendances
                .FirstOrDefault(a =>
                    a.MemberId == memberId &&
                    a.CheckOutTime == null);

            if (existingAttendance != null)
            {
                TempData["CheckInError"] =
                    "This member is already checked in.";

                return RedirectToAction("Scanner");
            }

            // Create attendance
            var attendance = new Attendance
            {
                MemberId = memberId,
                CheckInTime = DateTime.Now,
                CheckOutTime = null
            };

            _context.Attendances.Add(attendance);

            _context.SaveChanges();

            TempData["CheckInSuccess"] =
                $"ACCESS GRANTED — Welcome {member.Name}!";

            return RedirectToAction("Scanner");
        }



        // =========================================================
        // MEMBERSHIP APPLICATIONS
        // =========================================================

        [HttpGet]
        public IActionResult MembershipApplications()
        {
            var applications = _context.MembershipApplications
                .Include(a => a.Member)
                .OrderByDescending(a => a.ApplicationDate)
                .ToList();

            return View(applications);
        }


        // =========================================================
        // APPROVE MEMBERSHIP APPLICATION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveMembership(int id)
        {
            var application =
                _context.MembershipApplications
                    .Include(a => a.Member)
                    .FirstOrDefault(a =>
                        a.MembershipApplicationId == id);

            if (application == null)
            {
                TempData["Error"] =
                    "Membership application could not be found.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }


            // Make sure it is still pending

            if (application.Status != "Pending")
            {
                TempData["Error"] =
                    "This membership application has already been reviewed.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }


            // =====================================================
            // DETERMINE MEMBERSHIP DATES
            // =====================================================

            DateTime startDate = DateTime.Today;

            DateTime endDate;

            if (application.MembershipType == "Semester")
            {
                endDate = startDate.AddMonths(6);
            }
            else if (application.MembershipType == "Annual")
            {
                endDate = startDate.AddYears(1);
            }
            else
            {
                TempData["Error"] =
                    "Invalid membership type.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }


            // =====================================================
            // CHECK FOR EXISTING ACTIVE MEMBERSHIP
            // =====================================================

            var existingMembership =
                _context.Memberships
                    .FirstOrDefault(m =>
                        m.MemberId == application.MemberId &&
                        m.Status == "Active" &&
                        m.EndDate.Date >= DateTime.Today);

            if (existingMembership != null)
            {
                TempData["Error"] =
                    "This student already has an active membership.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }


            // =====================================================
            // CREATE MEMBERSHIP
            // =====================================================

            var membership = new Membership
            {
                MemberId =
                    application.MemberId,

                MembershipType =
                    application.MembershipType,

                StartDate =
                    startDate,

                EndDate =
                    endDate,

                Status =
                    "Active",

                Price =
                    application.Price,

                PaymentMethod =
                    application.PaymentMethod,

                IsRenewal =
                    false
            };


            _context.Memberships.Add(membership);


            // =====================================================
            // UPDATE APPLICATION
            // =====================================================

            application.Status = "Approved";

            application.ReviewedDate =
                DateTime.Now;

            application.AdminComment =
                "Membership application approved.";


            _context.SaveChanges();


            TempData["Success"] =
                "Membership application approved successfully.";

            return RedirectToAction(
                nameof(MembershipApplications));
        }


        // =========================================================
        // REJECT MEMBERSHIP APPLICATION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectMembership(
            int id,
            string adminComment)
        {
            var application =
                _context.MembershipApplications
                    .FirstOrDefault(a =>
                        a.MembershipApplicationId == id);

            if (application == null)
            {
                TempData["Error"] =
                    "Membership application could not be found.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }


            // Make sure it is still pending

            if (application.Status != "Pending")
            {
                TempData["Error"] =
                    "This membership application has already been reviewed.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }


            // Require a reason

            if (string.IsNullOrWhiteSpace(adminComment))
            {
                TempData["Error"] =
                    "Please provide a reason for rejecting the application.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }


            // =====================================================
            // UPDATE APPLICATION
            // =====================================================

            application.Status =
                "Rejected";

            application.ReviewedDate =
                DateTime.Now;

            application.AdminComment =
                adminComment.Trim();


            _context.SaveChanges();


            TempData["Success"] =
                "Membership application rejected.";

            return RedirectToAction(
                nameof(MembershipApplications));
        }
        // =========================================================
        // MANAGE MEMBERS
        // =========================================================

        public async Task<IActionResult> Members()
        {
            var members = await _context.Members
                .OrderBy(m => m.FirstName)
                .ThenBy(m => m.LastName)
                .ToListAsync();

            return View(members);
        }
        // =========================================================
        // VIEW RESERVATIONS
        // =========================================================

        public async Task<IActionResult> Reservations()
        {
            var reservations = await _context.Reservations
                .Join(
                    _context.Members,
                    reservation => reservation.MemberID,
                    member => member.MemberId,
                    (reservation, member) => new
                    {
                        Reservation = reservation,
                        Member = member
                    })
                .Join(
                    _context.Equipment,
                    x => x.Reservation.EquipmentID,
                    equipment => equipment.EquipmentID,
                    (x, equipment) => new AdminReservationViewModel
                    {
                        ReservationID = x.Reservation.ReservationID,
                        MemberName = x.Member.FirstName + " " + x.Member.LastName,
                        StudentNumber = x.Member.StaffStudentNumber,
                        EquipmentName = equipment.EquipmentName,
                        ReservationDate = x.Reservation.ReservationDate,
                        Status = x.Reservation.Status
                    })
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            return View(reservations);
        }
        // ============================
        // ADD EQUIPMENT
        // ============================

        [HttpGet]
        public IActionResult AddEquipment()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEquipment(Equipment equipment)
        {
            if (!ModelState.IsValid)
            {
                return View(equipment);
            }

            equipment.IsAvailable = true;

            _context.Equipment.Add(equipment);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Equipment added successfully.";

            return RedirectToAction(nameof(Equipment));
        }
        // ============================
        // REMOVE EQUIPMENT
        // ============================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveEquipment(int id)
        {
            var equipment = await _context.Equipment
                .FirstOrDefaultAsync(e => e.EquipmentID == id);

            if (equipment == null)
            {
                TempData["Error"] = "Equipment was not found.";

                return RedirectToAction(nameof(Equipment));
            }

            // Check if the equipment has reservations
            var hasReservations = await _context.Reservations
                .AnyAsync(r => r.EquipmentID == id);

            if (hasReservations)
            {
                TempData["Error"] =
                    "This equipment cannot be removed because it has reservation records.";

                return RedirectToAction(nameof(Equipment));
            }

            _context.Equipment.Remove(equipment);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Equipment removed successfully.";

            return RedirectToAction(nameof(Equipment));
        }
    }
}