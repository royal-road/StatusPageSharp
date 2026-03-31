using System.ComponentModel.DataAnnotations;

namespace StatusPageSharp.Application.Models.Admin;

public sealed class SiteSettingsAdminModel
{
    [Required]
    [StringLength(120)]
    public string SiteTitle { get; set; } = "StatusPageSharp";

    [Url]
    public string? LogoUrl { get; set; }

    [Range(15, 3600)]
    public int PublicRefreshIntervalSeconds { get; set; } = 60;

    [Range(1, 3650)]
    public int DefaultRawRetentionDays { get; set; } = 30;
}
