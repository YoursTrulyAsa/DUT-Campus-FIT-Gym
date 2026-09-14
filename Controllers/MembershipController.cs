using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class MembershipController : Controller
    {
        private readonly GymDbContext _context;
        private readonly MembershipPricingService _pricingService;

        public MembershipController(
            GymDbContext context,
            MembershipPricingService pricingService)
        {
            _context = context;
            _pricingService = pricingService;
        }

        [HttpGet]
        public async Task<IActionResult> Applications()
        {
            var applications =
                await _context.MembershipApplications
                    .Include(a => a.Member)
                    .OrderByDescending(a =>
                        a.MembershipApplicationId)
                    .ToListAsync();

            return View(applications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMembership(int id)
        {
            var application =
                await _context.MembershipApplications
                    .Include(a => a.Member)
                    .FirstOrDefaultAsync(a =>
                        a.MembershipApplicationId == id);

            if (application == null)
            {
                TempData["Error"] =
                    "Membership application could not be found.";

                return RedirectToAction(nameof(Applications));
            }

            if (application.Status != "Pending")
            {
                TempData["Error"] =
                    "This membership application has already been reviewed.";

                return RedirectToAction(nameof(Applications));
            }

            var existingMembership =
                await _context.Memberships
                    .Where(m =>
                        m.MemberId == application.MemberId &&
                        (m.Status == "WaitingForPayment" ||
                         m.Status == "Active"))
                    .OrderByDescending(m => m.MembershipId)
                    .FirstOrDefaultAsync();

            if (existingMembership != null)
            {
                TempData["Error"] =
                    "This member already has an active or unpaid membership.";

                return RedirectToAction(nameof(Applications));
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

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Membership application approved. The member can now proceed with payment.";

            return RedirectToAction(nameof(Applications));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectMembership(
            int id,
            string? adminComment)
        {
            var application =
                await _context.MembershipApplications
                    .FirstOrDefaultAsync(a =>
                        a.MembershipApplicationId == id);

            if (application == null)
            {
                TempData["Error"] =
                    "Membership application could not be found.";

                return RedirectToAction(nameof(Applications));
            }

            if (application.Status != "Pending")
            {
                TempData["Error"] =
                    "This membership application has already been reviewed.";

                return RedirectToAction(nameof(Applications));
            }

            application.Status = "Rejected";
            application.AdminComment = adminComment;
            application.ReviewedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Membership application rejected.";

            return RedirectToAction(nameof(Applications));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var memberships =
                await _context.Memberships
                    .Include(m => m.Member)
                    .OrderByDescending(m =>
                        m.MembershipId)
                    .ToListAsync();

            return View(memberships);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int id)
        {
            return RedirectToAction(
                nameof(ApproveMembership),
                new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(
            int id,
            string? reason)
        {
            return RedirectToAction(
                nameof(RejectMembership),
                new
                {
                    id,
                    adminComment = reason
                });
        }
    }
}