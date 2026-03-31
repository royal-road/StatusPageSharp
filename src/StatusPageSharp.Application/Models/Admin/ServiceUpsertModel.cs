using System.ComponentModel.DataAnnotations;
using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Admin;

public sealed class ServiceUpsertModel : IValidatableObject
{
    [Required]
    public Guid ServiceGroupId { get; set; }

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsEnabled { get; set; } = true;

    [Range(10, 3600)]
    public int CheckPeriodSeconds { get; set; } = 60;

    [Range(1, 20)]
    public int FailureThreshold { get; set; } = 3;

    [Range(1, 20)]
    public int RecoveryThreshold { get; set; } = 3;

    public int? RawRetentionDaysOverride { get; set; }

    [Required]
    public MonitorType MonitorType { get; set; }

    public string? Host { get; set; }

    public int? Port { get; set; }

    public string? Url { get; set; }

    [Required]
    [StringLength(20)]
    public string HttpMethod { get; set; } = "GET";

    public string? RequestHeadersJson { get; set; }

    public string? RequestBody { get; set; }

    [Required]
    [StringLength(80)]
    public string ExpectedStatusCodes { get; set; } = "200-299";

    [StringLength(500)]
    public string? ExpectedResponseSubstring { get; set; }

    public bool VerifyTlsCertificate { get; set; } = true;

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 10;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MonitorType == MonitorType.Tcp)
        {
            if (string.IsNullOrWhiteSpace(Host))
            {
                yield return new ValidationResult("TCP monitors require a host.", [nameof(Host)]);
            }

            if (Port is null or < 1 or > 65535)
            {
                yield return new ValidationResult(
                    "TCP monitors require a valid port.",
                    [nameof(Port)]
                );
            }
        }

        if (MonitorType is MonitorType.Http or MonitorType.Https && string.IsNullOrWhiteSpace(Url))
        {
            yield return new ValidationResult(
                "HTTP and HTTPS monitors require a URL.",
                [nameof(Url)]
            );
        }
    }
}
