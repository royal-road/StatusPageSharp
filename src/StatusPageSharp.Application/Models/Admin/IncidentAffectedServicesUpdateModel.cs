using System.ComponentModel.DataAnnotations;

namespace StatusPageSharp.Application.Models.Admin;

public sealed class IncidentAffectedServicesUpdateModel
{
    [Required]
    public List<IncidentAffectedServiceInputModel> Services { get; set; } = [];
}
