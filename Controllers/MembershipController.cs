using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    public class MembershipController : Controller
    {
        private readonly GymDbContext _context;

        public MembershipController(GymDbContext context)
        {
            _context = context;
        }


        // =========================================================
        // VIEW ALL MEMBERSHIP APPLICATIONS
        // =========================================================

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


        // =========================================================
        // APPROVE MEMBERSHIP APPLICATION
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMembership(
            int id)
        {
            var application =
                await _context.MembershipApplications
                    .FirstOrDefaultAsync(a =>
                        a.MembershipApplicationId == id);

            if (application == null)
            {
                TempData["Error"] =
                    "Membership application could not be found.";

                return RedirectToAction(
                    nameof(Applications));
            }


            // -----------------------------------------------------
            // ONLY PENDING APPLICATIONS CAN BE APPROVED
            // -----------------------------------------------------

            if (application.Status != "Pending")
            {
                TempData["Error"] =
                    "This membership application has already been reviewed.";

                return RedirectToAction(
                    nameof(Applications));
            }


            // -----------------------------------------------------
            // APPROVE APPLICATION
            // -----------------------------------------------------
            //
            // IMPORTANT:
            // We do NOT create an Active membership here.
            //
            // The student still needs to pay.
            //
            // The application therefore moves to:
            //
            // WaitingForPayment
            //
            // PayFast will later create/activate the real
            // Membership record after successful payment.
            // -----------------------------------------------------

            application.Status =
                "WaitingForPayment";


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Membership application approved. " +
                "The student can now proceed with payment.";


            return RedirectToAction(
                nameof(Applications));
        }


        // =========================================================
        // REJECT MEMBERSHIP APPLICATION
        // =========================================================

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

                return RedirectToAction(
                    nameof(Applications));
            }


            // -----------------------------------------------------
            // ONLY PENDING APPLICATIONS CAN BE REJECTED
            // -----------------------------------------------------

            if (application.Status != "Pending")
            {
                TempData["Error"] =
                    "This membership application has already been reviewed.";

                return RedirectToAction(
                    nameof(Applications));
            }


            // -----------------------------------------------------
            // REJECT APPLICATION
            // -----------------------------------------------------

            application.Status =
                "Rejected";


            // -----------------------------------------------------
            // SAVE ADMIN COMMENT
            // -----------------------------------------------------
            //
            // This assumes MembershipApplication has an
            // AdminComment property.
            //
            // If your model does not have this property,
            // remove the next line.
            // -----------------------------------------------------

            application.AdminComment =
                adminComment;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Membership application rejected.";


            return RedirectToAction(
                nameof(Applications));
        }


        // =========================================================
        // VIEW ALL ACTIVE MEMBERSHIPS
        // =========================================================
        //
        // This is separate from Applications.
        //
        // Applications = admin approval stage
        //
        // Memberships = actual paid/active memberships
        // =========================================================

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


        // =========================================================
        // OLD APPROVE ROUTE
        // =========================================================
        //
        // Kept only so existing links/forms don't break.
        //
        // New approval should use ApproveMembership().
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int id)
        {
            return RedirectToAction(
                nameof(ApproveMembership),
                new { id });
        }


        // =========================================================
        // OLD REJECT ROUTE
        // =========================================================
        //
        // Kept only so existing links/forms don't break.
        // =========================================================

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
