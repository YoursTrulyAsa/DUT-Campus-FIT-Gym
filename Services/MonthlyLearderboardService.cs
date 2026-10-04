using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace DUT_Campus_FIT_Gym.Services
{
    public class MonthlyLeaderboardService
    {
        private readonly GymDbContext _context;
        private readonly IConfiguration _configuration;

        public MonthlyLeaderboardService(
            GymDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<(bool Success, string Message)> FinalizeMonthAsync(
            int year,
            int month)
        {
            if (month < 1 || month > 12)
            {
                return (
                    false,
                    "Invalid month.");
            }

            DateTime startDate =
                new DateTime(
                    year,
                    month,
                    1);

            DateTime endDate =
                startDate.AddMonths(1);

            bool alreadyFinalized =
                await _context.MonthlyLeaderboards
                    .AnyAsync(x =>
                        x.Year == year &&
                        x.Month == month);

            if (alreadyFinalized)
            {
                return (
                    false,
                    "This month has already been finalized.");
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

            if (!members.Any())
            {
                return (
                    false,
                    "There are no members to finalize.");
            }

            var finalEntries =
                new List<MonthlyLeaderboard>();

            for (int i = 0;
                 i < members.Count;
                 i++)
            {
                var member =
                    members[i];

                bool isTopThree =
                    i < 3;

                bool isBottom =
                    i == members.Count - 1;

                finalEntries.Add(
                    new MonthlyLeaderboard
                    {
                        Year =
                            year,

                        Month =
                            month,

                        MemberId =
                            member.MemberId,

                        Rank =
                            i + 1,

                        Points =
                            member.Points,

                        Standing =
                            isTopThree
                                ? "Top 3"
                                : isBottom
                                    ? "Bottom"
                                    : "Normal",

                        GiftStatus =
                            isTopThree
                                ? "Pending"
                                : "NotApplicable",

                        NotificationSent =
                            false,

                        FinalizedAt =
                            DateTime.Now
                    });
            }

            _context.MonthlyLeaderboards.AddRange(
                finalEntries);

            await _context.SaveChangesAsync();

            var topThree =
                finalEntries
                    .Where(x =>
                        x.Rank <= 3)
                    .ToList();

            var bottom =
                finalEntries
                    .OrderByDescending(x =>
                        x.Rank)
                    .FirstOrDefault();

            foreach (var entry in topThree)
            {
                var member =
                    members.FirstOrDefault(
                        x =>
                            x.MemberId ==
                            entry.MemberId);

                if (member == null ||
                    string.IsNullOrWhiteSpace(
                        member.Email))
                {
                    continue;
                }

                try
                {
                    SendTopThreeEmail(
                        member.Name ?? "Member",
                        member.Email,
                        year,
                        month,
                        entry.Rank,
                        entry.Points);

                    entry.NotificationSent =
                        true;
                }
                catch
                {
                }
            }

            if (bottom != null &&
                !topThree.Any(
                    x =>
                        x.MemberId ==
                        bottom.MemberId))
            {
                var bottomMember =
                    members.FirstOrDefault(
                        x =>
                            x.MemberId ==
                            bottom.MemberId);

                if (bottomMember != null &&
                    !string.IsNullOrWhiteSpace(
                        bottomMember.Email))
                {
                    try
                    {
                        SendBottomEmail(
                            bottomMember.Name ??
                                "Member",
                            bottomMember.Email,
                            year,
                            month,
                            bottom.Points);

                        bottom.NotificationSent =
                            true;
                    }
                    catch
                    {
                    }
                }
            }

            await _context.SaveChangesAsync();

            return (
                true,
                $"{startDate:MMMM yyyy} has been finalized successfully.");
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