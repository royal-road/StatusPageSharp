using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Domain.Entities;

public class Service
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ServiceGroupId { get; set; }

    public ServiceGroup ServiceGroup { get; set; } = null!;

    public required string Name { get; set; }

    public required string Slug { get; set; }

    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsEnabled { get; set; } = true;

    public int CheckPeriodSeconds { get; set; } = 60;

    public int FailureThreshold { get; set; } = 3;

    public int RecoveryThreshold { get; set; } = 3;

    public int? RawRetentionDaysOverride { get; set; }

    public int ConsecutiveFailureCount { get; set; }

    public int ConsecutiveSuccessCount { get; set; }

    public DateTime? LastCheckStartedUtc { get; set; }

    public DateTime? LastCheckCompletedUtc { get; set; }

    public DateTime? NextCheckUtc { get; set; }

    public int? LastLatencyMilliseconds { get; set; }

    public CheckFailureKind LastFailureKind { get; set; }

    public string? LastFailureMessage { get; set; }

    public MonitorDefinition MonitorDefinition { get; set; } = null!;

    public List<CheckResult> CheckResults { get; set; } = [];

    public List<IncidentAffectedService> IncidentAffectedServices { get; set; } = [];

    public List<ScheduledMaintenanceService> ScheduledMaintenanceServices { get; set; } = [];
}
