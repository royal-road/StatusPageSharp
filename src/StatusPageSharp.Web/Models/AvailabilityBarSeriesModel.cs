using StatusPageSharp.Application.Models.Public;

namespace StatusPageSharp.Web.Models;

public sealed record AvailabilityBarSeriesModel(
    IReadOnlyList<DailyStatusModel> DailyStatuses,
    decimal? LastThirtyDayUptimePercentage,
    bool AllowWrap,
    bool ShowScale
);
