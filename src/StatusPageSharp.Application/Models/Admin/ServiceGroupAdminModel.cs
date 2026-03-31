namespace StatusPageSharp.Application.Models.Admin;

public sealed record ServiceGroupAdminModel(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    int DisplayOrder,
    int ServiceCount
);
