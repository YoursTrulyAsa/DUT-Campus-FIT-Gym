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

        // =========================
        // VIEW ALL MEMBERSHIPS
        // =========================

        public IActionResult Index()
        {
            var memberships = _context.Memberships
                .Include(m => m.Member)
                .OrderByDescending(m => m.MembershipId)
                .ToList();

            return View(memberships);
        }

        // =========================
        // APPROVE MEMBERSHIP
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Approve(int id)
        {
            var membership = _context.Memberships
                .FirstOrDefault(m => m.MembershipId == id);

            if (membership == null)
            {
                return NotFound();
            }

            membership.Status = "WaitingForPayment";

            _context.SaveChanges();

            TempData["SuccessMessage"] =
                "Membership approved. The student can now make payment.";

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // REJECT MEMBERSHIP
        // =========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Reject(int id)
        {
            var membership = _context.Memberships
                .FirstOrDefault(m => m.MembershipId == id);

            if (membership == null)
            {
                return NotFound();
            }

            membership.Status = "Rejected";

            _context.SaveChanges();

            TempData["SuccessMessage"] =
                "Membership rejected.";

            return RedirectToAction(nameof(Index));
        }
    }
}