using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Public;

public sealed record PublicIncidentServiceModel(
    Guid ServiceId,
    string ServiceName,
    IncidentImpactLevel ImpactLevel,
    bool IsResolved,
    DateTime? ResolvedUtc
);
