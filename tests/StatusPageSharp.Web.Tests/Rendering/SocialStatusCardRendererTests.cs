using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Domain.Enums;
using StatusPageSharp.Web.Rendering;

namespace StatusPageSharp.Web.Tests.Rendering;

public class SocialStatusCardRendererTests
{
    [Fact]
    public void BuildVersionToken_ReturnsSameToken_ForSameRenderedCardState()
    {
        var siteSummary = CreateSiteSummary(ServiceStatus.Operational);

        var token = SocialStatusCardRenderer.BuildVersionToken(siteSummary);
        var duplicateToken = SocialStatusCardRenderer.BuildVersionToken(siteSummary);

        Assert.Equal(token, duplicateToken);
    }

    [Fact]
    public void BuildVersionToken_ReturnsDifferentToken_WhenRenderedServiceStatusChanges()
    {
        var siteSummary = CreateSiteSummary(ServiceStatus.Operational);
        var changedSiteSummary = CreateSiteSummary(ServiceStatus.MajorOutage);

        var token = SocialStatusCardRenderer.BuildVersionToken(siteSummary);
        var changedToken = SocialStatusCardRenderer.BuildVersionToken(changedSiteSummary);

        Assert.NotEqual(token, changedToken);
    }

    private static PublicSiteSummaryModel CreateSiteSummary(ServiceStatus serviceStatus) =>
        new(
            "StatusPageSharp",
            null,
            30,
            serviceStatus,
            [
                new PublicServiceGroupModel(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    "Core",
                    "core",
                    null,
                    serviceStatus,
                    [
                        new PublicServiceSummaryModel(
                            Guid.Parse("22222222-2222-2222-2222-222222222222"),
                            "API",
                            "api",
                            null,
                            serviceStatus,
                            100,
                            99.95m,
                            []
                        ),
                    ]
                ),
            ],
            [],
            []
        );
}
