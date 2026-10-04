using Microsoft.Extensions.DependencyInjection;

namespace DUT_Campus_FIT_Gym.Services
{
    public class MonthlyLeaderboardBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<MonthlyLeaderboardBackgroundService> _logger;

        public MonthlyLeaderboardBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<MonthlyLeaderboardBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now =
                        DateTime.Now;

                    var previousMonth =
                        new DateTime(
                            now.Year,
                            now.Month,
                            1)
                        .AddMonths(-1);

                    using var scope =
                        _scopeFactory
                            .CreateScope();

                    var leaderboardService =
                        scope.ServiceProvider
                            .GetRequiredService<
                                MonthlyLeaderboardService>();

                    var result =
                        await leaderboardService
                            .FinalizeMonthAsync(
                                previousMonth.Year,
                                previousMonth.Month);

                    if (result.Success)
                    {
                        _logger.LogInformation(
                            "Automatic leaderboard finalization completed: {Message}",
                            result.Message);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Automatic leaderboard finalization check: {Message}",
                            result.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "An error occurred during automatic monthly leaderboard finalization.");
                }

                await Task.Delay(
                    TimeSpan.FromHours(1),
                    stoppingToken);
            }
        }
    }
}