using System.ComponentModel.DataAnnotations;
using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Admin;

public sealed class IncidentAffectedServiceInputModel
{
    [Required]
    public Guid ServiceId { get; set; }

    [Required]
    public IncidentImpactLevel ImpactLevel { get; set; } = IncidentImpactLevel.MajorOutage;
}
