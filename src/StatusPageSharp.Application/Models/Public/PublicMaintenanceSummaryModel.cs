namespace StatusPageSharp.Application.Models.Public;

public sealed record PublicMaintenanceSummaryModel(
    Guid Id,
    string Title,
    string Summary,
    DateTime StartsUtc,
    DateTime EndsUtc,
    IReadOnlyList<string> ServiceNames
);
