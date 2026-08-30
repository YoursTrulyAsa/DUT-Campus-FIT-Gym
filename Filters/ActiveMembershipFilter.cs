using DUT_Campus_FIT_Gym.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace DUT_Campus_FIT_Gym.Filters
{
    public class ActiveMembershipFilter : IAsyncActionFilter
    {
        private readonly GymDbContext _context;

        public ActiveMembershipFilter(GymDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // Get logged-in member ID
            var memberIdClaim = context.HttpContext.User
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(memberIdClaim, out int memberId))
            {
                context.Result = new RedirectToActionResult(
                    "Login",
                    "Account",
                    null);

                return;
            }

            // Find active membership
            var activeMembership = _context.Memberships
                .Where(m =>
                    m.MemberId == memberId &&
                    m.Status == "Active" &&
                    m.EndDate.HasValue &&
                    m.EndDate.Value.Date >= DateTime.Today)
                .OrderByDescending(m => m.MembershipId)
                .FirstOrDefault();

            // No active membership
            if (activeMembership == null)
            {
                context.HttpContext.Session.SetString(
                    "MembershipBlockedMessage",
                    "Your gym membership is inactive or expired. Please apply for or renew your membership to access this feature.");

                context.Result = new RedirectToActionResult(
                    "Membership",
                    "Member",
                    null);

                return;
            }

            // Membership is active
            await next();
        }
    }
}
