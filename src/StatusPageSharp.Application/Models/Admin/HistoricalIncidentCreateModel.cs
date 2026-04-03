using System.ComponentModel.DataAnnotations;

namespace StatusPageSharp.Application.Models.Admin;

public sealed class HistoricalIncidentCreateModel
{
    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Summary { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? InitialEventBody { get; set; }

    [StringLength(4000)]
    public string? Postmortem { get; set; }

    [Required]
    public DateTime StartedUtc { get; set; }

    public DateTime? ResolvedUtc { get; set; }

    [Required]
    public List<IncidentAffectedServiceInputModel> Services { get; set; } = [];
}
