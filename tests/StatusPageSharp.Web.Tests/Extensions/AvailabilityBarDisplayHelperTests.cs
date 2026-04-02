using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Web.Extensions;

namespace StatusPageSharp.Web.Tests.Extensions;

public class AvailabilityBarDisplayHelperTests
{
    [Theory]
    [InlineData(true, 100, "availability-bar-bad")]
    [InlineData(false, 99.5, "availability-bar-warn")]
    [InlineData(false, 100, "availability-bar-ok")]
    public void ToCssClass_ReturnsExpectedClass_ForDailyStatus(
        bool hasIncidents,
        decimal uptimePercentage,
        string expectedCssClass
    )
    {
        var dailyStatus = new DailyStatusModel(
            new DateOnly(2026, 4, 2),
            uptimePercentage >= 100m,
            hasIncidents,
            uptimePercentage
        );

        var cssClass = AvailabilityBarDisplayHelper.ToCssClass(dailyStatus);

        Assert.Equal(expectedCssClass, cssClass);
    }

    [Theory]
    [InlineData(true, 100, "Incident recorded")]
    [InlineData(false, 99.5, "Downtime recorded without an incident")]
    [InlineData(false, 100, "No incidents or downtime recorded")]
    public void ToSummary_ReturnsExpectedSummary_ForDailyStatus(
        bool hasIncidents,
        decimal uptimePercentage,
        string expectedSummary
    )
    {
        var dailyStatus = new DailyStatusModel(
            new DateOnly(2026, 4, 2),
            uptimePercentage >= 100m,
            hasIncidents,
            uptimePercentage
        );

        var summary = AvailabilityBarDisplayHelper.ToSummary(dailyStatus);

        Assert.Equal(expectedSummary, summary);
    }
}
