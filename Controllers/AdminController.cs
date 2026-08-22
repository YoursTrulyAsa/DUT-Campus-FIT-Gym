using Microsoft.AspNetCore.Authorization;
using DUT_Campus_FIT_Gym.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DUT_Campus_FIT_Gym.Controllers
{
    public class AdminController : Controller
    {
        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            return View();
        }
        private readonly GymDbContext _context;

        public AdminController(GymDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult MembershipApplications()
        {
            var applications = _context.Memberships
                .Include(m => m.Member)
                .Where(m => m.Status == "Pending")
                .ToList();

            return View(applications);
        }


    }
}
