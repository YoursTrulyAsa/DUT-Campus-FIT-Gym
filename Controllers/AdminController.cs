using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using DUT_Campus_FIT_Gym.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DUT_Campus_FIT_Gym.Controllers
{
    public class AdminController : Controller
    {
        private readonly GymDbContext _context;
        private readonly PasswordHasher<Member> _passwordHasher;
        private readonly RewardService _rewardService;

        public AdminController(
            GymDbContext context,
            RewardService rewardService)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Member>();
            _rewardService = rewardService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = new AdminDashboardViewModel
            {
                TotalMembers = await _context.Members
                    .CountAsync(),

                PendingApplications = await _context.MembershipApplications
                    .CountAsync(a => a.Status == "Pending"),

                ActiveMemberships = await _context.Memberships
                    .CountAsync(m =>
                        m.Status == "Active" &&
                        m.EndDate.HasValue &&
                        m.EndDate.Value.Date >= DateTime.Today),

                AvailableEquipment = await _context.Equipment
                        .CountAsync(e =>
                        !e.IsRetired &&
                        e.IsAvailable),

                UnavailableEquipment = await _context.Equipment
                         .CountAsync(e =>
                        !e.IsRetired &&
                        !e.IsAvailable),

                ActiveReservations = await _context.Reservations
                    .CountAsync(r => r.Status == "Active"),

                RecentApplications = await _context.MembershipApplications
                    .Include(a => a.Member)
                    .OrderByDescending(a => a.ApplicationDate)
                    .Take(5)
                    .ToListAsync()
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            return RedirectToAction(nameof(Index));
        }

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

            var member = new Member
            {
                Name = model.FirstName,
                Surname = model.LastName,
                StudentNumber = model.StaffStudentNumber,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                Role = "Trainer"
            };

            member.PasswordHash =
                _passwordHasher.HashPassword(
                    member,
                    model.Password);

            _context.Members.Add(member);

            _context.SaveChanges();

            var trainer = new Trainer
            {
                TrainerName =
                    $"{model.FirstName} {model.LastName}",

                Email = model.Email
            };

            _context.Trainers.Add(trainer);

            _context.SaveChanges();

            TempData["Success"] =
                "Trainer account created successfully.";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Equipment()
        {
            var equipment = await _context.Equipment
                .OrderBy(e => e.IsRetired)
                .ThenBy(e => e.EquipmentName)
                .ToListAsync();

            return View(equipment);
        }

        [HttpGet]
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

                return RedirectToAction(nameof(Scanner));
            }

            barcodeData = barcodeData.Trim();

            var member = _context.Members
                .FirstOrDefault(m =>
                    m.StudentNumber == barcodeData);

            if (member == null)
            {
                TempData["CheckInError"] =
                    "No member account was found for this barcode.";

                return RedirectToAction(nameof(Scanner));
            }

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

            if (membership.Status != "Active")
            {
                TempData["CheckInError"] =
                    $"{member.Name} {member.Surname} does not have an active membership.";

                return RedirectToAction(nameof(Scanner));
            }

            if (membership.EndDate.HasValue &&
                membership.EndDate.Value.Date < DateTime.Today)
            {
                TempData["CheckInError"] =
                    $"{member.Name} {member.Surname}'s membership has expired.";

                return RedirectToAction(nameof(Scanner));
            }

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

            var checkInTime = DateTime.Now;

            var checkInReward =
                   _rewardService.GetCheckInReward();

            var attendance = new Attendance
            {
                MemberId =
                    member.MemberId,

                CheckInTime =
                    checkInTime,

                CheckOutTime =
                    null
            };

            var reward = new RewardPoint
            {
                MemberId =
                    member.MemberId,

                Points =
                     checkInReward,

                Reason =
                    "Gym check-in",

                EarnedAt =
                    checkInTime
            };

            _context.Attendances.Add(
                attendance);

            _context.RewardPoints.Add(
                reward);

            _context.SaveChanges();

            TempData["CheckInSuccess"] =
                $"ACCESS GRANTED — Welcome {member.Name} {member.Surname}! You earned +1 reward point.";

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

        [HttpGet]
        public IActionResult MembershipApplications()
        {
            var applications = _context.MembershipApplications
                .Include(a => a.Member)
                .OrderByDescending(a => a.ApplicationDate)
                .ToList();

            return View(applications);
        }

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

            if (application.Status != "Pending")
            {
                TempData["Error"] =
                    "This membership application has already been reviewed.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }

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

            var validMembershipTypes = new[]
            {
                "Annual",
                "Semester1",
                "Semester2"
            };

            if (!validMembershipTypes.Contains(
                application.MembershipType))
            {
                TempData["Error"] =
                    "Invalid membership type.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }

            var membership = new Membership
            {
                MemberId = application.MemberId,
                MembershipType = application.MembershipType,
                FirstTimeMember = application.FirstTimeMember,
                BasePrice = application.BasePrice,
                DiscountPercentage = application.DiscountPercentage,
                Price = application.Price,
                StartDate = null,
                EndDate = null,
                Status = "WaitingForPayment",
                PaymentMethod = application.PaymentMethod,
                PaymentReference = null,
                PaymentDate = null,
                PaymentStatus = "Pending"
            };

            _context.Memberships.Add(membership);

            application.Status = "Approved";

            application.ReviewedDate = DateTime.Now;

            application.AdminComment =
                "Membership application approved. Awaiting payment.";

            _context.SaveChanges();

            TempData["Success"] =
                "Membership application approved successfully. The student can now complete payment.";

            return RedirectToAction(
                nameof(MembershipApplications));
        }

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

            if (application.Status != "Pending")
            {
                TempData["Error"] =
                    "This membership application has already been reviewed.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }

            if (string.IsNullOrWhiteSpace(adminComment))
            {
                TempData["Error"] =
                    "Please provide a reason for rejecting the application.";

                return RedirectToAction(
                    nameof(MembershipApplications));
            }

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

        [HttpGet]
        public async Task<IActionResult> Members()
        {
            var members = await _context.Members
                .OrderBy(m => m.Name)
                .ThenBy(m => m.Surname)
                .ToListAsync();

            return View(members);
        }

        [HttpGet]
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

        
        [HttpGet]
        public IActionResult AddEquipment()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEquipment(
            Equipment equipment,
            IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(equipment);
            }

            equipment.IsAvailable = true;
            equipment.IsRetired = false;

            if (imageFile != null && imageFile.Length > 0)
            {
                var allowedExtensions = new[]
                {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };

                var extension =
                    Path.GetExtension(imageFile.FileName)
                        .ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError(
                        "ImagePath",
                        "Only JPG, JPEG, PNG and WEBP images are allowed.");

                    return View(equipment);
                }

                var uploadFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "uploads",
                    "equipment");

                Directory.CreateDirectory(uploadFolder);

                var fileName =
                    $"{Guid.NewGuid()}{extension}";

                var filePath =
                    Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(
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

            return RedirectToAction(nameof(Equipment));
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetireEquipment(int id)
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

            if (equipment.IsRetired)
            {
                TempData["Error"] =
                    "This equipment has already been retired.";

                return RedirectToAction(nameof(Equipment));
            }

            var hasActiveReservation =
                await _context.Reservations
                    .AnyAsync(r =>
                        r.EquipmentID == id &&
                        r.Status == "Reserved" &&
                        r.EndTime > DateTime.Now);

            if (hasActiveReservation)
            {
                TempData["Error"] =
                    "This equipment cannot be retired while it has an active reservation.";

                return RedirectToAction(nameof(Equipment));
            }

            equipment.IsRetired = true;
            equipment.IsAvailable = false;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{equipment.EquipmentName} has been retired successfully.";

            return RedirectToAction(nameof(Equipment));
        }

        [HttpGet]
        public IActionResult AddStaff()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddStaff(CreateStaffViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

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

            staff.PasswordHash =
                _passwordHasher.HashPassword(
                    staff,
                    model.Password);

            _context.Members.Add(staff);

            _context.SaveChanges();

            TempData["Success"] =
                $"Staff account for {staff.Name} {staff.Surname} was created successfully.";

            return RedirectToAction(
                nameof(Members));
        }

        [HttpGet]
        public async Task<IActionResult> Announcements()
        {
            var announcements = await _context.Announcements
                .OrderByDescending(a => a.DatePosted)
                .ToListAsync();

            return View(
                "~/Views/Admin/Announcements.cshtml",
                announcements);
        }

        [HttpGet]
        public IActionResult AddAnnouncement()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAnnouncement(
            Announcement announcement)
        {
            if (!ModelState.IsValid)
            {
                return View(announcement);
            }

            announcement.DatePosted = DateTime.Now;

            if (string.IsNullOrWhiteSpace(
                announcement.Category))
            {
                announcement.Category = "General";
            }

            _context.Announcements.Add(announcement);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Announcement posted successfully.";

            return RedirectToAction(
                nameof(Announcements));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAnnouncement(
            int id)
        {
            var announcement =
                await _context.Announcements
                    .FirstOrDefaultAsync(a =>
                        a.AnnouncementID == id);

            if (announcement == null)
            {
                TempData["Error"] =
                    "Announcement was not found.";

                return RedirectToAction(
                    nameof(Announcements));
            }

            _context.Announcements.Remove(
                announcement);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Announcement deleted successfully.";

            return RedirectToAction(
                nameof(Announcements));
        }
    }
}