namespace StatusPageSharp.Domain.Entities;

public class ScheduledMaintenanceService
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ScheduledMaintenanceId { get; set; }

    public ScheduledMaintenance ScheduledMaintenance { get; set; } = null!;

    public Guid ServiceId { get; set; }

    public Service Service { get; set; } = null!;
}
