using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Infrastructure.Tests.Support;

public class TestPublicStatusService : IPublicStatusService
{
    public Task<PublicSiteSummaryModel> GetSiteSummaryAsync(CancellationToken cancellationToken) =>
        Task.FromResult(
            new PublicSiteSummaryModel(
                "Status Page",
                null,
                60,
                ServiceStatus.Operational,
                [],
                [],
                []
            )
        );

    public Task<PublicServiceDetailsModel?> GetServiceDetailsAsync(
        string slug,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();

    public Task<ServiceHistorySeriesModel?> GetServiceHistoryAsync(
        string slug,
        CancellationToken cancellationToken
    ) => throw new NotSupportedException();
}
