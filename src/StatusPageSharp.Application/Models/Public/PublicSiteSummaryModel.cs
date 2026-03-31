using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Public;

public sealed record PublicSiteSummaryModel(
    string SiteTitle,
    string? LogoUrl,
    int PublicRefreshIntervalSeconds,
    ServiceStatus SiteStatus,
    IReadOnlyList<PublicServiceGroupModel> Groups,
    IReadOnlyList<PublicIncidentSummaryModel> ActiveIncidents,
    IReadOnlyList<PublicMaintenanceSummaryModel> ActiveMaintenance
);
