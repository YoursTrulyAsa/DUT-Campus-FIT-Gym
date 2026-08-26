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

        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel
            {
                // ==========================================
                // MEMBERS
                // ==========================================

                TotalMembers = await _context.Members
                    .CountAsync(),


                // ==========================================
                // PENDING MEMBERSHIP APPLICATIONS
                // ==========================================

                PendingApplications = await _context.MembershipApplications
                    .CountAsync(a => a.Status == "Pending"),


                // ==========================================
                // ACTIVE MEMBERSHIPS
                // ==========================================

                ActiveMemberships = await _context.Memberships
                    .CountAsync(m =>
                        m.Status == "Active" &&
                        m.EndDate.HasValue &&
                        m.EndDate.Value.Date >= DateTime.Today),


                // ==========================================
                // AVAILABLE EQUIPMENT
                // ==========================================

                AvailableEquipment = await _context.Equipment
                    .CountAsync(e => e.IsAvailable),


                // ==========================================
                // UNAVAILABLE EQUIPMENT
                // ==========================================

                UnavailableEquipment = await _context.Equipment
                    .CountAsync(e => !e.IsAvailable),


                // ==========================================
                // ACTIVE RESERVATIONS
                // ==========================================

                ActiveReservations = await _context.Reservations
                    .CountAsync(r => r.Status == "Active"),


                // ==========================================
                // RECENT APPLICATIONS
                // ==========================================

                RecentApplications = await _context.MembershipApplications
                    .Include(a => a.Member)
                    .OrderByDescending(a => a.ApplicationDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }


        // =========================================================
        // ADD TRAINER
        // =========================================================

        [HttpGet]
        public IActionResult AddTrainer()
        {
            return View();
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
            // CHECK STAFF/STUDENT NUMBER
            // ==========================================

            bool numberExists = _context.Members
                .Any(m =>
                    m.StudentNumber == model.StaffStudentNumber);


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

                StudentNumber = model.StaffStudentNumber,

                Email = model.Email,

                PhoneNumber = model.PhoneNumber,

                Role = "Trainer"
            };


            // ==========================================
            // HASH PASSWORD
            // ==========================================

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

            return RedirectToAction(nameof(Index));
        }


        // =========================================================
        // MANAGE EQUIPMENT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Equipment()
        {
            var equipment = await _context.Equipment
                .OrderBy(e => e.EquipmentName)
                .ToListAsync();

            return View(equipment);
        }


        // =========================================================
        // SCANNER
        // =========================================================

        public IActionResult Scanner()
        {
            return View();
        }


        // =========================================================
        // VERIFY MEMBER BARCODE CHECK-IN
        // =========================================================
        //
        // The Virtual Gym Card contains the member's
        // StudentNumber as a Code 128 barcode.
        //
        // Example:
        //
        // 220123456
        //
        // Admin/Staff scans the barcode.
        // The system:
        // 1. Finds the member
        // 2. Checks their membership
        // 3. Checks expiry
        // 4. Checks whether they are already inside
        // 5. Records attendance
        // 6. Displays member information
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyBarcodeCheckIn(string barcodeData)
        {
            // =========================================================
            // VALIDATE BARCODE
            // =========================================================

            if (string.IsNullOrWhiteSpace(barcodeData))
            {
                TempData["CheckInError"] =
                    "No barcode was detected.";

                return RedirectToAction(nameof(Scanner));
            }

            barcodeData = barcodeData.Trim();


            // =========================================================
            // FIND MEMBER USING STUDENT/STAFF NUMBER
            // =========================================================

            var member = _context.Members
                .FirstOrDefault(m =>
                    m.StudentNumber == barcodeData);


            if (member == null)
            {
                TempData["CheckInError"] =
                    "No member account was found for this barcode.";

                return RedirectToAction(nameof(Scanner));
            }


            // =========================================================
            // FIND LATEST MEMBERSHIP
            // =========================================================

            var membership = _context.Memberships
                .Where(m =>
                    m.MemberId == member.MemberId)
                .OrderByDescending(m =>
                    m.MembershipId)
                .FirstOrDefault();


            if (membership == null)
            {
                TempData["CheckInError"] =
                    "This member does not have a gym membership.";

                return RedirectToAction(nameof(Scanner));
            }


            // =========================================================
            // CHECK MEMBERSHIP STATUS
            // =========================================================

            if (membership.Status != "Active")
            {
                TempData["CheckInError"] =
                    $"{member.Name} {member.Surname} does not have an active membership.";

                return RedirectToAction(nameof(Scanner));
            }


            // =========================================================
            // CHECK MEMBERSHIP EXPIRY
            // =========================================================

            if (membership.EndDate.HasValue &&
                membership.EndDate.Value.Date < DateTime.Today)
            {
                TempData["CheckInError"] =
                    $"{member.Name} {member.Surname}'s membership has expired.";

                return RedirectToAction(nameof(Scanner));
            }


            // =========================================================
            // CHECK IF MEMBER IS ALREADY CHECKED IN
            // =========================================================

            var existingAttendance = _context.Attendances
                .FirstOrDefault(a =>
                    a.MemberId == member.MemberId &&
                    a.CheckOutTime == null);


            if (existingAttendance != null)
            {
                TempData["CheckInError"] =
                    $"{member.Name} {member.Surname} is already checked in.";

                return RedirectToAction(nameof(Scanner));
            }


            // =========================================================
            // CREATE ATTENDANCE
            // =========================================================

            var attendance = new Attendance
            {
                MemberId = member.MemberId,

                CheckInTime = DateTime.Now,

                CheckOutTime = null
            };


            _context.Attendances.Add(attendance);

            _context.SaveChanges();


            // =========================================================
            // SAVE SUCCESS INFORMATION
            // =========================================================

            TempData["CheckInSuccess"] =
                $"ACCESS GRANTED — Welcome {member.Name} {member.Surname}!";


            TempData["ScannedMemberName"] =
                $"{member.Name} {member.Surname}";


            TempData["ScannedStudentNumber"] =
                member.StudentNumber;


            TempData["ScannedMembershipType"] =
                membership.MembershipType;


            TempData["ScannedMembershipStatus"] =
                membership.Status;


            TempData["ScannedExpiryDate"] =
                membership.EndDate.HasValue
                    ? membership.EndDate.Value.ToString("dd MMM yyyy")
                    : "N/A";


            TempData["ScannedCheckInTime"] =
                attendance.CheckInTime.ToString(
                    "dd MMM yyyy, HH:mm");


            return RedirectToAction(nameof(Scanner));
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

        // =========================================================
        // APPROVE MEMBERSHIP APPLICATION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveMembership(int id)
        {
            var application = _context.MembershipApplications
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


            // =========================================================
            // MUST STILL BE PENDING
            // =========================================================

            if (application.Status != "Pending")
            {
                TempData["Error"] =
                    "This membership application has already been reviewed.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }


            // =========================================================
            // CHECK FOR CURRENT ACTIVE MEMBERSHIP
            // =========================================================

            var existingActiveMembership =
                _context.Memberships
                    .FirstOrDefault(m =>
                        m.MemberId == application.MemberId &&
                        m.Status == "Active" &&
                        m.EndDate.HasValue &&
                        m.EndDate.Value.Date >= DateTime.Today);

            if (existingActiveMembership != null)
            {
                TempData["Error"] =
                    "This student already has an active membership.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }


            // =========================================================
            // CHECK IF MEMBERSHIP WAS ALREADY CREATED
            // =========================================================

            var existingMembership =
                _context.Memberships
                    .FirstOrDefault(m =>
                        m.MemberId == application.MemberId &&
                        m.MembershipType == application.MembershipType &&
                        m.Price == application.Price &&
                        m.Status == "WaitingForPayment");

            if (existingMembership != null)
            {
                TempData["Error"] =
                    "A payment-pending membership already exists for this application.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }


            // =========================================================
            // DETERMINE MEMBERSHIP DATES
            // =========================================================

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


            // =========================================================
            // CREATE MEMBERSHIP
            // =========================================================
            //
            // IMPORTANT:
            //
            // Approval NEVER makes the membership Active.
            //
            // It MUST remain WaitingForPayment until PayFast
            // confirms the payment.
            //

            var membership = new Membership
            {
                MemberId = application.MemberId,

                MembershipType = application.MembershipType,

                StartDate = startDate,

                EndDate = endDate,

                Status = "WaitingForPayment",

                Price = application.Price,

                PaymentMethod = application.PaymentMethod
            };


            _context.Memberships.Add(membership);


            // =========================================================
            // UPDATE APPLICATION
            // =========================================================

            application.Status = "Approved";

            application.ReviewedDate = DateTime.Now;

            application.AdminComment =
                "Membership application approved. Awaiting payment.";


            _context.SaveChanges();


            // =========================================================
            // SUCCESS
            // =========================================================

            TempData["Success"] =
                "Membership application approved successfully. The student can now complete payment.";

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


            // ==========================================
            // MAKE SURE IT IS STILL PENDING
            // ==========================================

            if (application.Status != "Pending")
            {
                TempData["Error"] =
                    "This membership application has already been reviewed.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }


            // ==========================================
            // REQUIRE A REASON
            // ==========================================

            if (string.IsNullOrWhiteSpace(adminComment))
            {
                TempData["Error"] =
                    "Please provide a reason for rejecting the application.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }


            // ==========================================
            // UPDATE APPLICATION
            // ==========================================

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
                .OrderBy(m => m.Name)
                .ThenBy(m => m.Surname)
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
                        ReservationID =
                            x.Reservation.ReservationID,

                        MemberName =
                            x.Member.Name + " " + x.Member.Surname,

                        StudentNumber =
                            x.Member.StudentNumber,

                        EquipmentName =
                            equipment.EquipmentName,

                        ReservationDate =
                            x.Reservation.ReservationDate,

                        Status =
                            x.Reservation.Status
                    })
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();

            return View(reservations);
        }


        // =========================================================
        // ADD EQUIPMENT
        // =========================================================

        [HttpGet]
        public IActionResult AddEquipment()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEquipment(
            Equipment equipment)
        {
            if (!ModelState.IsValid)
            {
                return View(equipment);
            }


            equipment.IsAvailable = true;

            _context.Equipment.Add(equipment);

            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Equipment added successfully.";

            return RedirectToAction(nameof(Equipment));
        }


        // =========================================================
        // REMOVE EQUIPMENT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveEquipment(int id)
        {
            var equipment = await _context.Equipment
                .FirstOrDefaultAsync(e =>
                    e.EquipmentID == id);


            if (equipment == null)
            {
                TempData["Error"] =
                    "Equipment was not found.";

                return RedirectToAction(nameof(Equipment));
            }


            // ==========================================
            // CHECK FOR RESERVATIONS
            // ==========================================

            var hasReservations =
                await _context.Reservations
                    .AnyAsync(r =>
                        r.EquipmentID == id);


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

        // =========================================================
        // ADD STAFF - GET
        // =========================================================

        [HttpGet]
        public IActionResult AddStaff()
        {
            return View();
        }


        // =========================================================
        // ADD STAFF - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddStaff(CreateStaffViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }


            // =========================================================
            // CHECK EMAIL
            // =========================================================

            bool emailExists =
                _context.Members
                    .Any(m => m.Email == model.Email);

            if (emailExists)
            {
                ModelState.AddModelError(
                    "Email",
                    "This email is already registered.");

                return View(model);
            }


            // =========================================================
            // CHECK STAFF NUMBER
            // =========================================================

            bool numberExists =
                _context.Members
                    .Any(m =>
                        m.StudentNumber ==
                        model.StaffStudentNumber);

            if (numberExists)
            {
                ModelState.AddModelError(
                    "StaffStudentNumber",
                    "This staff number is already registered.");

                return View(model);
            }


            // =========================================================
            // CREATE STAFF MEMBER
            // =========================================================

            var staff = new Member
            {
                Name =
                    model.FirstName.Trim(),

                Surname =
                    model.LastName.Trim(),

                StudentNumber =
                    model.StaffStudentNumber.Trim(),

                Email =
                    model.Email.Trim(),

                PhoneNumber =
                    model.PhoneNumber.Trim(),

                Role =
                    "Staff"
            };


            // =========================================================
            // HASH PASSWORD
            // =========================================================

            staff.PasswordHash =
                _passwordHasher.HashPassword(
                    staff,
                    model.Password);


            // =========================================================
            // SAVE STAFF ACCOUNT
            // =========================================================

            _context.Members.Add(staff);

            _context.SaveChanges();


            // =========================================================
            // SUCCESS
            // =========================================================

            TempData["Success"] =
                $"Staff account for {staff.Name} {staff.Surname} was created successfully.";

            return RedirectToAction(
                nameof(Members));
        }
    }
}