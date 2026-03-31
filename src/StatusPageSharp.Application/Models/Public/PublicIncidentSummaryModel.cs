using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Public;

public sealed record PublicIncidentSummaryModel(
    Guid Id,
    string Title,
    string Summary,
    IncidentStatus Status,
    DateTime StartedUtc,
    DateTime? ResolvedUtc,
    IReadOnlyList<string> AffectedServiceNames
);
