using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Web.Extensions;

namespace StatusPageSharp.Web.Metadata;

public static class SocialMetadataBuilder
{
    public static string BuildSiteDescription(PublicSiteSummaryModel siteSummary)
    {
        var monitoredServices = siteSummary.Groups.Sum(group => group.Services.Count);
        var segments = new List<string>
        {
            $"{StatusDisplayHelper.ToLabel(siteSummary.SiteStatus)} across {monitoredServices} monitored {Pluralize("service", monitoredServices)}",
        };

        if (siteSummary.ActiveIncidents.Count > 0)
        {
            segments.Add(
                $"{siteSummary.ActiveIncidents.Count} active {Pluralize("incident", siteSummary.ActiveIncidents.Count)}"
            );
        }

        if (siteSummary.ActiveMaintenance.Count > 0)
        {
            segments.Add(
                $"{siteSummary.ActiveMaintenance.Count} current maintenance {Pluralize("window", siteSummary.ActiveMaintenance.Count)}"
            );
        }

        return $"{string.Join(". ", segments)}.";
    }

    public static string BuildServiceDescription(PublicServiceDetailsModel service)
    {
        var uptimeSummary = service.LastThirtyDayUptimePercentage is decimal uptimePercentage
            ? $"{uptimePercentage:0.00}% uptime over the last 30 days"
            : "recent uptime data is unavailable";

        return $"{StatusDisplayHelper.ToLabel(service.Status)} for {service.Name}. {uptimeSummary}.";
    }

    public static string BuildIncidentDescription(PublicIncidentDetailsModel incident)
    {
        var affectedServices = incident.AffectedServices.Count;
        var resolution = incident.ResolvedUtc is null
            ? "Ongoing incident."
            : $"Resolved {incident.ResolvedUtc:MMM d, yyyy HH:mm 'UTC'}.";

        return $"{incident.Summary} {affectedServices} affected {Pluralize("service", affectedServices)}. {resolution}";
    }

    public static string BuildIncidentHistoryDescription(PublicIncidentHistoryPageModel history)
    {
        return history.TotalCount == 0
            ? "No resolved incidents have been published yet."
            : $"{history.TotalCount} resolved {Pluralize("incident", history.TotalCount)} across monitored services.";
    }

    public static string BuildMaintenanceDescription(
        IReadOnlyList<PublicMaintenanceSummaryModel> maintenanceItems
    )
    {
        return maintenanceItems.Count == 0
            ? "No scheduled maintenance windows."
            : $"{maintenanceItems.Count} scheduled maintenance {Pluralize("window", maintenanceItems.Count)} across monitored services.";
    }

    private static string Pluralize(string noun, int count) => count == 1 ? noun : $"{noun}s";
}
