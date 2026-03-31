namespace StatusPageSharp.Domain.Entities;

public class HourlyServiceRollup
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ServiceId { get; set; }

    public Service Service { get; set; } = null!;

    public DateTime HourStartUtc { get; set; }

    public int TotalChecks { get; set; }

    public int SuccessfulChecks { get; set; }

    public int AvailabilityEligibleChecks { get; set; }

    public int AvailabilitySuccessChecks { get; set; }

    public int TotalLatencyMilliseconds { get; set; }

    public int AverageLatencyMilliseconds { get; set; }

    public int P95LatencyMilliseconds { get; set; }

    public int DowntimeMinutes { get; set; }

    public int MaintenanceMinutes { get; set; }
}
