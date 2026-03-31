using System.ComponentModel.DataAnnotations;

namespace StatusPageSharp.Application.Models.Admin;

public sealed class MaintenanceUpsertModel
{
    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Summary { get; set; } = string.Empty;

    [Required]
    public DateTime StartsUtc { get; set; }

    [Required]
    public DateTime EndsUtc { get; set; }

    public int TimeZoneOffsetMinutes { get; set; }

    public List<Guid> ServiceIds { get; set; } = [];
}
