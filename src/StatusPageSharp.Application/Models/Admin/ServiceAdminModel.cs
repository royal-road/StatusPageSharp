using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Admin;

public sealed record ServiceAdminModel(
    Guid Id,
    Guid ServiceGroupId,
    string GroupName,
    string Name,
    string Slug,
    string? Description,
    bool IsEnabled,
    int DisplayOrder,
    int CheckPeriodSeconds,
    int FailureThreshold,
    int RecoveryThreshold,
    int? RawRetentionDaysOverride,
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
