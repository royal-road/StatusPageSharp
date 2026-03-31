using System.ComponentModel.DataAnnotations;

namespace StatusPageSharp.Application.Models.Admin;

public sealed class IncidentEventUpsertModel
{
    [Required]
    [StringLength(120)]
    public string Message { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Body { get; set; }
}
