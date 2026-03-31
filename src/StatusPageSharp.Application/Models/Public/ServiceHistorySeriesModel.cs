namespace StatusPageSharp.Application.Models.Public;

public sealed record ServiceHistorySeriesModel(
    string ServiceName,
    IReadOnlyList<HistoryPointModel> UptimePoints,
    IReadOnlyList<HistoryPointModel> LatencyPoints,
    IReadOnlyList<MonthlySlaPointModel> SlaPoints
);
