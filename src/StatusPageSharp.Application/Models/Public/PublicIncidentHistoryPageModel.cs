namespace StatusPageSharp.Application.Models.Public;

public sealed record PublicIncidentHistoryPageModel(
    IReadOnlyList<PublicIncidentSummaryModel> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages
);
