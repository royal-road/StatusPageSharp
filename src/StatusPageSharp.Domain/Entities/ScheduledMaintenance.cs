namespace StatusPageSharp.Domain.Entities;

public class ScheduledMaintenance
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Title { get; set; }

    public required string Summary { get; set; }

    public DateTime StartsUtc { get; set; }

    public DateTime EndsUtc { get; set; }

    public string? CreatedByUserId { get; set; }

    public List<ScheduledMaintenanceService> Services { get; set; } = [];
}
