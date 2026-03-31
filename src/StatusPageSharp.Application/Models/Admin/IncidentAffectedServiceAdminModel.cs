using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Admin;

public sealed record IncidentAffectedServiceAdminModel(
    Guid ServiceId,
    string ServiceName,
    IncidentImpactLevel ImpactLevel,
    DateTime AddedUtc,
    bool IsResolved,
    DateTime? ResolvedUtc
);
