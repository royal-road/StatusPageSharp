using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Admin;

public sealed record AdminDashboardModel(
    int ServiceCount,
    int GroupCount,
    int OpenIncidentCount,
    int ActiveMaintenanceCount,
    ServiceStatus SiteStatus
);
