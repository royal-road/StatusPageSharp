namespace StatusPageSharp.Application.Models.Public;

public sealed record PublicIncidentEventModel(DateTime CreatedUtc, string Message, string? Body);
