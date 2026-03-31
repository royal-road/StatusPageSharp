using StatusPageSharp.Application.Models.Admin;

namespace StatusPageSharp.Application.Abstractions;

public interface IAdminCatalogService
{
    Task<AdminDashboardModel> GetDashboardAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ServiceGroupAdminModel>> GetServiceGroupsAsync(
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<ServiceAdminModel>> GetServicesAsync(CancellationToken cancellationToken);

    Task<ServiceAdminModel?> GetServiceAsync(Guid id, CancellationToken cancellationToken);

    Task CreateServiceGroupAsync(
        ServiceGroupUpsertModel model,
        CancellationToken cancellationToken
    );

    Task UpdateServiceGroupAsync(
        Guid id,
        ServiceGroupUpsertModel model,
        CancellationToken cancellationToken
    );

    Task CreateServiceAsync(ServiceUpsertModel model, CancellationToken cancellationToken);

    Task UpdateServiceAsync(Guid id, ServiceUpsertModel model, CancellationToken cancellationToken);
}
