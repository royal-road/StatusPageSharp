namespace StatusPageSharp.Application.Models.Public;

public sealed record PublicIncidentDetailsModel(
    Guid Id,
    string Title,
    string Summary,
    DateTime StartedUtc,
    DateTime? ResolvedUtc,
    string? Postmortem,
    IReadOnlyList<PublicIncidentServiceModel> AffectedServices,
    IReadOnlyList<PublicIncidentEventModel> Events
);
