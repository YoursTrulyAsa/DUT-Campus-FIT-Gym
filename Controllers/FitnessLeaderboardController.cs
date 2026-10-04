using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using DUT_Campus_FIT_Gym.ViewModels;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using DUT_Campus_FIT_Gym.Services;

namespace DUT_Campus_FIT_Gym.Controllers
{
    [Authorize(Roles = "Admin,Trainer")]
    public class FitnessLeaderboardController : Controller
    {
        private readonly GymDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly MonthlyLeaderboardService _monthlyLeaderboardService;

        public FitnessLeaderboardController(
    GymDbContext context,
    IConfiguration configuration,
    MonthlyLeaderboardService monthlyLeaderboardService)
        {
            _context = context;
            _configuration = configuration;
            _monthlyLeaderboardService =
                monthlyLeaderboardService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int? year,
            int? month)
        {
            DateTime today = DateTime.Today;

            int selectedYear = year ?? today.Year;
            int selectedMonth = month ?? today.Month;

            if (selectedMonth < 1 || selectedMonth > 12)
            {
                selectedMonth = today.Month;
            }

            DateTime startDate =
                new DateTime(
                    selectedYear,
                    selectedMonth,
                    1);

            DateTime endDate =
                startDate.AddMonths(1);

            var finalizedEntries =
                await _context.MonthlyLeaderboards
                    .Include(x => x.Member)
                    .Where(x =>
                        x.Year == selectedYear &&
                        x.Month == selectedMonth)
                    .OrderBy(x => x.Rank)
                    .ToListAsync();

            if (finalizedEntries.Any())
            {
                var finalizedModel =
                    new MonthlyLeaderboardViewModel
                    {
                        Year = selectedYear,

                        Month = selectedMonth,

                        MonthName =
                            startDate.ToString("MMMM yyyy"),

                        IsFinalized = true,

                        FinalizedAt =
                            finalizedEntries
                                .First()
                                .FinalizedAt,

                        Entries =
                            finalizedEntries
                                .Select(x =>
                                    new MonthlyLeaderboardEntryViewModel
                                    {
                                        MonthlyLeaderboardId =
                                            x.MonthlyLeaderboardId,

                                        Rank =
                                            x.Rank,

                                        MemberId =
                                            x.MemberId,

                                        Name =
                                            x.Member?.Name ?? "",

                                        Surname =
                                            x.Member?.Surname ?? "",

                                        Email =
                                            x.Member?.Email ?? "",

                                        Points =
                                            x.Points,

                                        Standing =
                                            x.Standing,

                                        GiftStatus =
                                            x.GiftStatus,

                                        NotificationSent =
                                            x.NotificationSent
                                    })
                                .ToList()
                    };

                return View(
                    "Index",
                    finalizedModel);
            }

            var members =
                await _context.Members
                    .Where(m =>
                        m.Role == "Student" ||
                        m.Role == "Staff")
                    .Select(m => new
                    {
                        m.MemberId,
                        m.Name,
                        m.Surname,
                        m.Email,

                        Points =
                            _context.RewardPoints
                                .Where(r =>
                                    r.MemberId == m.MemberId &&
                                    r.EarnedAt >= startDate &&
                                    r.EarnedAt < endDate)
                                .Select(r => (int?)r.Points)
                                .Sum() ?? 0
                    })
                    .OrderByDescending(x => x.Points)
                    .ThenBy(x => x.Surname)
                    .ThenBy(x => x.Name)
                    .ToListAsync();

            var entries =
                new List<MonthlyLeaderboardEntryViewModel>();

            for (int i = 0;
                 i < members.Count;
                 i++)
            {
                var member =
                    members[i];

                entries.Add(
                    new MonthlyLeaderboardEntryViewModel
                    {
                        MonthlyLeaderboardId = 0,

                        Rank =
                            i + 1,

                        MemberId =
                            member.MemberId,

                        Name =
                            member.Name ?? "",

                        Surname =
                            member.Surname ?? "",

                        Email =
                            member.Email ?? "",

                        Points =
                            member.Points,

                        Standing =
                            i < 3
                                ? "Top 3"
                                : i == members.Count - 1
                                    ? "Bottom"
                                    : "Normal",

                        GiftStatus =
                            i < 3
                                ? "Pending"
                                : "NotApplicable",

                        NotificationSent =
                            false
                    });
            }

            var model =
                new MonthlyLeaderboardViewModel
                {
                    Year =
                        selectedYear,

                    Month =
                        selectedMonth,

                    MonthName =
                        startDate.ToString("MMMM yyyy"),

                    IsFinalized =
                        false,

                    Entries =
                        entries
                };

            return View(
                "Index",
                model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FinalizeMonth(
    int year,
    int month)
        {
            var result =
                await _monthlyLeaderboardService
                    .FinalizeMonthAsync(
                        year,
                        month);

            if (result.Success)
            {
                TempData["LeaderboardSuccess"] =
                    result.Message;
            }
            else
            {
                TempData["LeaderboardError"] =
                    result.Message;
            }

            return RedirectToAction(
                "Index",
                new
                {
                    year,
                    month
                });
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkGiftCollected(
            int id)
        {
            var entry =
                await _context.MonthlyLeaderboards
                    .FirstOrDefaultAsync(x =>
                        x.MonthlyLeaderboardId == id &&
                        x.Standing == "Top 3");

            if (entry == null)
            {
                return NotFound();
            }

            if (entry.GiftStatus == "Collected")
            {
                return RedirectToAction(
                    "Index",
                    new
                    {
                        year = entry.Year,
                        month = entry.Month
                    });
            }

            entry.GiftStatus =
                "Collected";

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Index",
                new
                {
                    year = entry.Year,
                    month = entry.Month
                });
        }

        private void SendTopThreeEmail(
            string name,
            string email,
            int year,
            int month,
            int rank,
            int points)
        {
            var smtpSettings =
                _configuration
                    .GetSection("SmtpSettings");

            var message =
                new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    "DUT Campus FIT Gym",
                    smtpSettings["SenderEmail"]));

            message.To.Add(
                new MailboxAddress(
                    name,
                    email));

            message.Subject =
                $"DUT Campus FIT Gym - {new DateTime(year, month, 1):MMMM yyyy} Fitness Challenge Results";

            var builder =
                new BodyBuilder();

            builder.TextBody =
                $"Hello {name},\n\n" +
                $"Congratulations! You finished #{rank} on the DUT Campus FIT Gym fitness leaderboard for {new DateTime(year, month, 1):MMMM yyyy}.\n\n" +
                $"Your final score was {points} reward points.\n\n" +
                "You are one of the Top 3 members for the month and qualify for the monthly fitness challenge gift.\n\n" +
                "Please visit the gym administration desk to collect your gift.\n\n" +
                "Regards,\n" +
                "DUT Campus FIT Gym";

            message.Body =
                builder.ToMessageBody();

            using var client =
                new SmtpClient();

            client.ServerCertificateValidationCallback =
                (s, c, h, e) => true;

            client.Connect(
                smtpSettings["Server"],
                int.Parse(
                    smtpSettings["Port"]),
                SecureSocketOptions.StartTls);

            client.Authenticate(
                smtpSettings["SenderEmail"],
                smtpSettings["Password"]);

            client.Send(message);

            client.Disconnect(true);
        }

        private void SendBottomEmail(
            string name,
            string email,
            int year,
            int month,
            int points)
        {
            var smtpSettings =
                _configuration
                    .GetSection("SmtpSettings");

            var message =
                new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    "DUT Campus FIT Gym",
                    smtpSettings["SenderEmail"]));

            message.To.Add(
                new MailboxAddress(
                    name,
                    email));

            message.Subject =
                $"DUT Campus FIT Gym - {new DateTime(year, month, 1):MMMM yyyy} Fitness Update";

            var builder =
                new BodyBuilder();

            builder.TextBody =
                $"Hello {name},\n\n" +
                $"The DUT Campus FIT Gym fitness leaderboard for {new DateTime(year, month, 1):MMMM yyyy} has been finalized.\n\n" +
                $"Your final score for the month was {points} reward points.\n\n" +
                "This is a reminder to stay active and keep working towards your fitness goals. You can earn more reward points by participating in available gym challenges.\n\n" +
                "Keep going and do your best next month!\n\n" +
                "Regards,\n" +
                "DUT Campus FIT Gym";

            message.Body =
                builder.ToMessageBody();

            using var client =
                new SmtpClient();

            client.ServerCertificateValidationCallback =
                (s, c, h, e) => true;

            client.Connect(
                smtpSettings["Server"],
                int.Parse(
                    smtpSettings["Port"]),
                SecureSocketOptions.StartTls);

            client.Authenticate(
                smtpSettings["SenderEmail"],
                smtpSettings["Password"]);

            client.Send(message);

            client.Disconnect(true);
        }
    }
}