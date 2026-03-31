using System.ComponentModel.DataAnnotations;

namespace StatusPageSharp.Application.Models.Admin;

public sealed class ManualIncidentUpsertModel
{
    [Required]
    [StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Summary { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? InitialEventBody { get; set; }

    public List<IncidentAffectedServiceInputModel> Services { get; set; } = [];
}
