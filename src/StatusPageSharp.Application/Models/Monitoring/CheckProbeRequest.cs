using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Monitoring;

public sealed record CheckProbeRequest(
    Guid ServiceId,
    MonitorType MonitorType,
    string? Host,
    int? Port,
    string? Url,
    string HttpMethod,
    string? RequestHeadersJson,
    string? RequestBody,
    string ExpectedStatusCodes,
    string? ExpectedResponseSubstring,
    bool VerifyTlsCertificate,
    int TimeoutSeconds
);
