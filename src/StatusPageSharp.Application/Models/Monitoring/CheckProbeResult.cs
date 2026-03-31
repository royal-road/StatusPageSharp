using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Monitoring;

public sealed record CheckProbeResult(
    bool IsSuccess,
    int DurationMilliseconds,
    int? HttpStatusCode,
    CheckFailureKind FailureKind,
    string? FailureMessage,
    string? ResponseSnippet
);
