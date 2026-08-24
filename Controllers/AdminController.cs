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
                FirstName = model.FirstName,

                LastName = model.LastName,

                StaffStudentNumber =
                    model.StaffStudentNumber,

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