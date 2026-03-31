namespace StatusPageSharp.Domain.Entities;

public class MonthlySlaSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ServiceId { get; set; }

    public Service Service { get; set; } = null!;

    public DateOnly Month { get; set; }

    public int EligibleMinutes { get; set; }

    public int DowntimeMinutes { get; set; }

    public int MaintenanceMinutes { get; set; }

    public decimal UptimePercentage { get; set; }

    public DateTime ComputedUtc { get; set; }
}
