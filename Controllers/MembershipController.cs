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

            if (application.Status != "Pending")
            {
                TempData["Error"] =
                    "This membership application has already been reviewed.";

                return RedirectToAction(
                    nameof(Applications));
            }


            application.Status =
                "WaitingForPayment";


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Membership application approved. " +
                "The student can now proceed with payment.";


            return RedirectToAction(
                nameof(Applications));
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

                return RedirectToAction(
                    nameof(Applications));
            }

            if (application.Status != "Pending")
            {
                TempData["Error"] =
                    "This membership application has already been reviewed.";

                return RedirectToAction(
                    nameof(Applications));
            }

            application.Status =
                "Rejected";


            application.AdminComment =
                adminComment;


            await _context.SaveChangesAsync();


            TempData["Success"] =
                "Membership application rejected.";


            return RedirectToAction(
                nameof(Applications));
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
