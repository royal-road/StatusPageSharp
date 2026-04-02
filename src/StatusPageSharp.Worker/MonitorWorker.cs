using StatusPageSharp.Application.Abstractions;

namespace StatusPageSharp.Worker;

public sealed class MonitorWorker(
    IMonitoringCoordinator monitoringCoordinator,
    ILogger<MonitorWorker> logger,
    TimeProvider timeProvider
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var checkTimer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        var lastDerivedRefreshUtc = DateTime.MinValue;
        var lastRetentionCleanupUtc = DateTime.MinValue;

        while (await checkTimer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var executedChecks = await monitoringCoordinator.ExecuteDueChecksAsync(
                    stoppingToken
                );
                if (executedChecks > 0)
                {
                    logger.LogInformation("Executed {CheckCount} due checks.", executedChecks);
                }

                var now = timeProvider.GetUtcNow().UtcDateTime;
                if (now - lastDerivedRefreshUtc >= TimeSpan.FromMinutes(5))
                {
                    await monitoringCoordinator.RefreshDerivedDataAsync(stoppingToken);
                    lastDerivedRefreshUtc = now;
                }

                if (now - lastRetentionCleanupUtc >= TimeSpan.FromHours(1))
                {
                    await monitoringCoordinator.CleanupExpiredCheckResultsAsync(stoppingToken);
                    lastRetentionCleanupUtc = now;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "The monitoring worker loop failed.");
            }
        }
    }
}
