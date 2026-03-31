namespace StatusPageSharp.Domain.Entities;

public class SiteSetting
{
    public int Id { get; set; }

    public string SiteTitle { get; set; } = "StatusPageSharp";

    public string? LogoUrl { get; set; }

    public int PublicRefreshIntervalSeconds { get; set; } = 60;

    public int DefaultRawRetentionDays { get; set; } = 30;
}
