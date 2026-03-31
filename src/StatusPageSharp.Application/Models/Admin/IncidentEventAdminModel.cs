using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Application.Models.Admin;

public sealed record IncidentEventAdminModel(
    Guid Id,
    IncidentEventType EventType,
    string Message,
    string? Body,
    bool IsSystemGenerated,
    DateTime CreatedUtc,
    string? CreatedByUserId
);
