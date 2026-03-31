using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Web.Extensions;

public static class StatusDisplayHelper
{
    public static string ToCssClass(ServiceStatus status)
    {
        return status switch
        {
            ServiceStatus.Operational => "status-ok",
            ServiceStatus.PartialOutage => "status-warn",
            ServiceStatus.MajorOutage => "status-bad",
            ServiceStatus.UnderMaintenance => "status-maintenance",
            _ => "status-unknown",
        };
    }

    public static string ToLabel(ServiceStatus status)
    {
        return status switch
        {
            ServiceStatus.Operational => "Operational",
            ServiceStatus.PartialOutage => "Partial Outage",
            ServiceStatus.MajorOutage => "Major Outage",
            ServiceStatus.UnderMaintenance => "Scheduled Maintenance",
            _ => "Unknown",
        };
    }
}
