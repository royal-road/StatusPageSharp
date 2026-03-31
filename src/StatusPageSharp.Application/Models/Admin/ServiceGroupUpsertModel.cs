using System.ComponentModel.DataAnnotations;

namespace StatusPageSharp.Application.Models.Admin;

public sealed class ServiceGroupUpsertModel
{
    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }
}
