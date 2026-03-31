using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Public;

public sealed record PublicServiceDetailsModel(
    Guid Id,
    Guid GroupId,
    string GroupName,
    string Name,
    string Slug,
    string? Description,
    ServiceStatus Status,
    int? LastLatencyMilliseconds,
    IReadOnlyList<HistoryPointModel> UptimePoints,
    IReadOnlyList<HistoryPointModel> LatencyPoints,
    IReadOnlyList<MonthlySlaPointModel> SlaPoints
);
