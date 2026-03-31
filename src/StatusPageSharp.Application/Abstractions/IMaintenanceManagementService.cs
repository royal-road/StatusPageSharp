using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Application.Models.Public;

namespace StatusPageSharp.Application.Abstractions;

public interface IMaintenanceManagementService
{
    Task<IReadOnlyList<PublicMaintenanceSummaryModel>> GetPublicMaintenanceAsync(
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<MaintenanceAdminModel>> GetAdminMaintenanceAsync(
        CancellationToken cancellationToken
    );

    Task CreateMaintenanceAsync(
        MaintenanceUpsertModel model,
        string? userId,
        CancellationToken cancellationToken
    );

    Task UpdateMaintenanceAsync(
        Guid id,
        MaintenanceUpsertModel model,
        CancellationToken cancellationToken
    );

    Task DeleteMaintenanceAsync(Guid id, CancellationToken cancellationToken);
}
