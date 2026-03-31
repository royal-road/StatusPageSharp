using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Public;

public sealed record PublicServiceGroupModel(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    ServiceStatus Status,
    IReadOnlyList<PublicServiceSummaryModel> Services
);
