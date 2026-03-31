using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Domain.Entities;

public class IncidentAffectedService
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid IncidentId { get; set; }

    public Incident Incident { get; set; } = null!;

    public Guid ServiceId { get; set; }

    public Service Service { get; set; } = null!;

    public IncidentImpactLevel ImpactLevel { get; set; }

    public DateTime AddedUtc { get; set; }

    public DateTime? ResolvedUtc { get; set; }

    public bool IsResolved { get; set; }
}
