using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Public;

public sealed record PublicServiceSummaryModel(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    ServiceStatus Status,
    int? LastLatencyMilliseconds,
    decimal? LastThirtyDayUptimePercentage
);
