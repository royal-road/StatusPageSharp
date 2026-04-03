using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Application.Models.Public;

namespace StatusPageSharp.Application.Abstractions;

public interface IIncidentManagementService
{
    Task<IReadOnlyList<PublicIncidentSummaryModel>> GetIncidentSummariesAsync(
        CancellationToken cancellationToken
    );

    Task<PublicIncidentHistoryPageModel> GetIncidentHistoryPageAsync(
        Guid? serviceId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken
    );

    Task<PublicIncidentDetailsModel?> GetIncidentAsync(
        Guid id,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<IncidentAdminModel>> GetAdminIncidentsAsync(
        CancellationToken cancellationToken
    );

    Task<IncidentAdminModel?> GetAdminIncidentAsync(Guid id, CancellationToken cancellationToken);

    Task<Guid> CreateManualIncidentAsync(
        ManualIncidentUpsertModel model,
        string? userId,
        CancellationToken cancellationToken
    );

    Task<Guid> CreateHistoricalIncidentAsync(
        HistoricalIncidentCreateModel model,
        string? userId,
        CancellationToken cancellationToken
    );

    Task AddManualEventAsync(
        Guid id,
        IncidentEventUpsertModel model,
        string? userId,
        CancellationToken cancellationToken
    );

    Task UpdateAffectedServicesAsync(
        Guid id,
        IncidentAffectedServicesUpdateModel model,
        string? userId,
        CancellationToken cancellationToken
    );

    Task ResolveIncidentAsync(
        Guid id,
        string? postmortem,
        string? userId,
        CancellationToken cancellationToken
    );

    Task MergeIncidentAsync(
        Guid targetId,
        Guid sourceId,
        string? userId,
        CancellationToken cancellationToken
    );

    Task DeleteIncidentAsync(Guid id, CancellationToken cancellationToken);

    Task UpdatePostmortemAsync(
        Guid id,
        string? postmortem,
        string? userId,
        CancellationToken cancellationToken
    );
}
