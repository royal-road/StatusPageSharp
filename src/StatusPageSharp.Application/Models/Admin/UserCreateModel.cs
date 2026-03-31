using System.ComponentModel.DataAnnotations;

namespace StatusPageSharp.Application.Models.Admin;

public sealed class UserCreateModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(120)]
    public string? DisplayName { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
