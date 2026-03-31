using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Domain.Entities;

public class CheckResult
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ServiceId { get; set; }

    public Service Service { get; set; } = null!;

    public DateTime StartedUtc { get; set; }

    public DateTime CompletedUtc { get; set; }

    public int DurationMilliseconds { get; set; }

    public bool IsSuccess { get; set; }

    public bool IsIgnoredForAvailability { get; set; }

    public int? HttpStatusCode { get; set; }

    public CheckFailureKind FailureKind { get; set; }

    public string? FailureMessage { get; set; }

    public string? ResponseSnippet { get; set; }
}
