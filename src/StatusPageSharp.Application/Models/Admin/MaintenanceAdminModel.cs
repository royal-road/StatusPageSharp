namespace StatusPageSharp.Application.Models.Admin;

public sealed record MaintenanceAdminModel(
    Guid Id,
    string Title,
    string Summary,
    DateTime StartsUtc,
    DateTime EndsUtc,
    IReadOnlyList<Guid> ServiceIds,
    IReadOnlyList<string> ServiceNames
);
