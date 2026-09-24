using DUT_Campus_FIT_Gym.Data;
using DUT_Campus_FIT_Gym.Models;
using Microsoft.EntityFrameworkCore;

namespace DUT_Campus_FIT_Gym.Services
{
    public class ReservationBackgroundService
        : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ReservationBackgroundService> _logger;

        public ReservationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<ReservationBackgroundService> logger)
        {
            _scopeFactory =
                scopeFactory;

            _logger =
                logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessReservationsAsync(
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error while processing equipment reservations.");
                }

                try
                {
                    await Task.Delay(
                        TimeSpan.FromSeconds(5),
                        stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task ProcessReservationsAsync(
            CancellationToken stoppingToken)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var context =
                scope.ServiceProvider
                    .GetRequiredService<GymDbContext>();

            var now =
                DateTime.Now;

            var reservations =
                await context.Reservations
                    .Where(r =>
                        r.Status == "Reserved" ||
                        r.Status == "Expired")
                    .ToListAsync(
                        stoppingToken);

            foreach (var reservation
                     in reservations)
            {
                if (reservation.Status ==
                        "Reserved" &&
                    reservation.EndTime <= now)
                {
                    reservation.Status =
                        "Expired";

                    reservation.NotificationDismissed =
                        false;

                    var equipment =
                        await context.Equipment
                            .FirstOrDefaultAsync(
                                e =>
                                    e.EquipmentID ==
                                    reservation.EquipmentID,
                                stoppingToken);

                    if (equipment != null)
                    {
                        equipment.IsAvailable =
                            false;
                    }
                }

                if (reservation.Status ==
                        "Expired" &&
                    !reservation.NotificationDismissed &&
                    reservation.EndTime
                        .AddMinutes(1) <= now)
                {
                    var penaltyReason =
                        $"Reservation notification penalty #{reservation.ReservationID}";

                    var penaltyAlreadyApplied =
                        await context.RewardPoints
                            .AnyAsync(
                                r =>
                                    r.MemberId ==
                                        reservation.MemberID &&
                                    r.Reason ==
                                        penaltyReason,
                                stoppingToken);

                    if (!penaltyAlreadyApplied)
                    {
                        var rewardPoint =
                            new RewardPoint
                            {
                                MemberId =
                                    reservation.MemberID,

                                Points =
                                    -15,

                                Reason =
                                    penaltyReason,

                                EarnedAt =
                                    now
                            };

                        context.RewardPoints.Add(
                            rewardPoint);
                    }

                    reservation.NotificationDismissed =
                        true;
                }

                if (reservation.Status ==
                        "Expired" &&
                    reservation.EndTime
                        .AddMinutes(2) <= now)
                {
                    var equipment =
                        await context.Equipment
                            .FirstOrDefaultAsync(
                                e =>
                                    e.EquipmentID ==
                                    reservation.EquipmentID,
                                stoppingToken);

                    if (equipment != null)
                    {
                        var newerReservationExists =
                            await context.Reservations
                                .AnyAsync(
                                    r =>
                                        r.EquipmentID ==
                                            reservation.EquipmentID &&
                                        r.Status ==
                                            "Reserved" &&
                                        r.ReservationDate >
                                            reservation.EndTime,
                                    stoppingToken);

                        if (!newerReservationExists)
                        {
                            equipment.IsAvailable =
                                true;
                        }
                    }
                }
            }

            await context.SaveChangesAsync(
                stoppingToken);
        }
    }
}