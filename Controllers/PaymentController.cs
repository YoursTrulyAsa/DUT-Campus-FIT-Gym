using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Student")]
    public class PaymentController : Controller
    {
        private readonly GymDbContext _context;

        public PaymentController(GymDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index(int id)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var membership = _context.Memberships
                .FirstOrDefault(m =>
                    m.MembershipId == id &&
                    m.MemberId == memberId);

            if (membership == null)
            {
                return NotFound();
            }

            if (membership.Status != "WaitingForPayment")
            {
                TempData["PaymentError"] =
                    "This membership is not currently available for payment.";

                return RedirectToAction(
                    "Membership",
                    "Member");
            }

            ViewBag.MembershipId =
                membership.MembershipId;

            ViewBag.Amount =
                membership.Price;

            ViewBag.MembershipType =
                membership.MembershipType;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessPayment(int membershipId)
        {
            var memberIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberIdClaim))
            {
                return RedirectToAction("Login", "Account");
            }

            int memberId = int.Parse(memberIdClaim);

            var membership = _context.Memberships
                .FirstOrDefault(m =>
                    m.MembershipId == membershipId &&
                    m.MemberId == memberId);

            if (membership == null)
            {
                return NotFound();
            }

            if (membership.Status != "WaitingForPayment")
            {
                TempData["PaymentError"] =
                    "This membership is not available for payment.";

                return RedirectToAction(
                    "Membership",
                    "Member");
            }

            membership.Status = "Active";

            _context.SaveChanges();

            TempData["PaymentSuccess"] =
                "Payment successful! Your gym membership is now active.";

            return RedirectToAction(
                "Membership",
                "Member");
        }
    }
}