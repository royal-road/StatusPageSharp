using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Domain.Entities;

public class MonitorDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ServiceId { get; set; }

    public Service Service { get; set; } = null!;

    public MonitorType MonitorType { get; set; }

    public string? Host { get; set; }

    public int? Port { get; set; }

    public string? Url { get; set; }

    public string HttpMethod { get; set; } = "GET";

    public string? RequestHeadersJson { get; set; }

    public string? RequestBody { get; set; }

    public string ExpectedStatusCodes { get; set; } = "200-299";

    public string? ExpectedResponseSubstring { get; set; }

    public bool VerifyTlsCertificate { get; set; } = true;

    public int TimeoutSeconds { get; set; } = 10;
}
