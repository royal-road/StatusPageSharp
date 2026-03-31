using StatusPageSharp.Application.Models.Monitoring;

namespace StatusPageSharp.Application.Abstractions;

public interface IMonitoringCoordinator
{
    Task<int> ExecuteDueChecksAsync(CancellationToken cancellationToken);

    Task RefreshDerivedDataAsync(CancellationToken cancellationToken);

    Task CleanupExpiredCheckResultsAsync(CancellationToken cancellationToken);

    Task<MonitorExecutionResult> ExecuteCheckAsync(
        Guid serviceId,
        CancellationToken cancellationToken
    );
}
