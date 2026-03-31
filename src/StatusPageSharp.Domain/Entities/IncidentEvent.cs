using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Domain.Entities;

public class IncidentEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid IncidentId { get; set; }

    public Incident Incident { get; set; } = null!;

    public IncidentEventType EventType { get; set; }

    public required string Message { get; set; }

    public string? Body { get; set; }

    public bool IsSystemGenerated { get; set; }

    public DateTime CreatedUtc { get; set; }

    public string? CreatedByUserId { get; set; }
}
