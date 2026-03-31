using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Monitoring;

public sealed record MonitorExecutionResult(
    Guid ServiceId,
    string ServiceName,
    bool IsSuccess,
    bool WasIgnoredForAvailability,
    int DurationMilliseconds,
    CheckFailureKind FailureKind,
    string? FailureMessage
);
