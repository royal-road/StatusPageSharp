using System.ComponentModel.DataAnnotations;

namespace StatusPageSharp.Application.Models.Admin;

public sealed class UserPasswordResetModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;
}
