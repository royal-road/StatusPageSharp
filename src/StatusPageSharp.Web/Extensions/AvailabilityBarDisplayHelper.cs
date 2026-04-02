using StatusPageSharp.Application.Models.Public;

namespace StatusPageSharp.Web.Extensions;

public static class AvailabilityBarDisplayHelper
{
    public static string ToCssClass(DailyStatusModel dailyStatus)
    {
        return dailyStatus switch
        {
            { HasIncidents: true } => "availability-bar-bad",
            { UptimePercentage: < 100m } => "availability-bar-warn",
            _ => "availability-bar-ok",
        };
    }

    public static string ToSummary(DailyStatusModel dailyStatus)
    {
        return dailyStatus switch
        {
            { HasIncidents: true } => "Incident recorded",
            { UptimePercentage: < 100m } => "Downtime recorded without an incident",
            _ => "No incidents or downtime recorded",
        };
    }
}
