using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Domain.Entities;

public class Incident
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public IncidentSource Source { get; set; }

    public IncidentStatus Status { get; set; } = IncidentStatus.Open;

    public required string Title { get; set; }

    public required string Summary { get; set; }

    public string? Postmortem { get; set; }

    public DateTime StartedUtc { get; set; }

    public DateTime? ResolvedUtc { get; set; }

    public string? CreatedByUserId { get; set; }

    public List<IncidentAffectedService> AffectedServices { get; set; } = [];

    public List<IncidentEvent> Events { get; set; } = [];
}
