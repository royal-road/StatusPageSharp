using StatusPageSharp.Application.Models.Public;

namespace StatusPageSharp.Application.Abstractions;

public interface IPublicStatusService
{
    Task<PublicSiteSummaryModel> GetSiteSummaryAsync(CancellationToken cancellationToken);

    Task<PublicServiceDetailsModel?> GetServiceDetailsAsync(
        string slug,
        CancellationToken cancellationToken
    );

    Task<ServiceHistorySeriesModel?> GetServiceHistoryAsync(
        string slug,
        CancellationToken cancellationToken
    );
}
