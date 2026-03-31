using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Application.Models.Public;

namespace StatusPageSharp.Application.Abstractions;

public interface IIncidentManagementService
{
    Task<IReadOnlyList<PublicIncidentSummaryModel>> GetIncidentSummariesAsync(
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
}
