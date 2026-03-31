using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Domain.Logic;

public static class StatusCalculator
{
    public static ServiceStatus CalculateServiceStatus(
        bool isUnderMaintenance,
        IReadOnlyCollection<IncidentImpactLevel> activeImpacts
    )
    {
        if (activeImpacts.Contains(IncidentImpactLevel.MajorOutage))
        {
            return ServiceStatus.MajorOutage;
        }

        if (activeImpacts.Contains(IncidentImpactLevel.PartialOutage))
        {
            return ServiceStatus.PartialOutage;
        }

        return isUnderMaintenance ? ServiceStatus.UnderMaintenance : ServiceStatus.Operational;
    }

    public static ServiceStatus CalculateGroupStatus(IReadOnlyCollection<ServiceStatus> statuses)
    {
        if (statuses.Count == 0)
        {
            return ServiceStatus.Operational;
        }

        var nonMaintenanceStatuses = statuses
            .Where(status => status != ServiceStatus.UnderMaintenance)
            .ToArray();

        if (nonMaintenanceStatuses.Length == 0)
        {
            return ServiceStatus.UnderMaintenance;
        }

        if (nonMaintenanceStatuses.All(status => status == ServiceStatus.MajorOutage))
        {
            return ServiceStatus.MajorOutage;
        }

        if (nonMaintenanceStatuses.Any(status => status != ServiceStatus.Operational))
        {
            return ServiceStatus.PartialOutage;
        }

        return ServiceStatus.Operational;
    }

    public static ServiceStatus CalculateSiteStatus(IReadOnlyCollection<ServiceStatus> statuses) =>
        CalculateGroupStatus(statuses);
}
