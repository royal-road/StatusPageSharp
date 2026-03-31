using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Admin;

public sealed record IncidentAdminModel(
    Guid Id,
    string Title,
    string Summary,
    IncidentSource Source,
    IncidentStatus Status,
    DateTime StartedUtc,
    DateTime? ResolvedUtc,
    string? Postmortem,
    IReadOnlyList<IncidentAffectedServiceAdminModel> AffectedServices,
    IReadOnlyList<IncidentEventAdminModel> Events
);
