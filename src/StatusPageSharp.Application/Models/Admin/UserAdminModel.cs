namespace StatusPageSharp.Application.Models.Admin;

public sealed record UserAdminModel(
    string Id,
    string Email,
    string? DisplayName,
    bool EmailConfirmed,
    bool IsEnabled,
    DateTime CreatedUtc
);
