using Microsoft.EntityFrameworkCore;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Domain.Entities;
using StatusPageSharp.Domain.Enums;
using StatusPageSharp.Infrastructure.Data;

namespace StatusPageSharp.Infrastructure.Services;

public sealed class AdminCatalogService(
    ApplicationDbContext dbContext,
    IPublicStatusService publicStatusService,
    TimeProvider timeProvider
) : IAdminCatalogService
{
    public async Task<AdminDashboardModel> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var siteSummary = await publicStatusService.GetSiteSummaryAsync(cancellationToken);
        var serviceCount = await dbContext.Services.CountAsync(cancellationToken);
        var groupCount = await dbContext.ServiceGroups.CountAsync(cancellationToken);
        var openIncidentCount = await dbContext.Incidents.CountAsync(
            incident => incident.Status == IncidentStatus.Open,
            cancellationToken
        );
        var activeMaintenanceCount = await dbContext.ScheduledMaintenances.CountAsync(
            item => item.StartsUtc <= now && item.EndsUtc > now,
            cancellationToken
        );

        return new AdminDashboardModel(
            serviceCount,
            groupCount,
            openIncidentCount,
            activeMaintenanceCount,
            siteSummary.SiteStatus
        );
    }

    public async Task<IReadOnlyList<ServiceGroupAdminModel>> GetServiceGroupsAsync(
        CancellationToken cancellationToken
    )
    {
        return await dbContext
            .ServiceGroups.AsNoTracking()
            .OrderBy(group => group.DisplayOrder)
            .ThenBy(group => group.Name)
            .Select(group => new ServiceGroupAdminModel(
                group.Id,
                group.Name,
                group.Slug,
                group.Description,
                group.DisplayOrder,
                group.Services.Count
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceAdminModel>> GetServicesAsync(
        CancellationToken cancellationToken
    )
    {
        var services = await dbContext
            .Services.AsNoTracking()
            .Include(service => service.ServiceGroup)
            .Include(service => service.MonitorDefinition)
            .OrderBy(service => service.ServiceGroup.DisplayOrder)
            .ThenBy(service => service.DisplayOrder)
            .ToListAsync(cancellationToken);

        return services.Select(MapService).ToList();
    }

    public async Task<ServiceAdminModel?> GetServiceAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var service = await dbContext
            .Services.AsNoTracking()
            .Include(service => service.ServiceGroup)
            .Include(service => service.MonitorDefinition)
            .Where(service => service.Id == id)
            .SingleOrDefaultAsync(cancellationToken);

        return service is null ? null : MapService(service);
    }

    public async Task CreateServiceGroupAsync(
        ServiceGroupUpsertModel model,
        CancellationToken cancellationToken
    )
    {
        var entity = new ServiceGroup
        {
            Name = model.Name.Trim(),
            Slug = model.Slug.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim(),
            DisplayOrder = model.DisplayOrder,
        };

        dbContext.ServiceGroups.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateServiceGroupAsync(
        Guid id,
        ServiceGroupUpsertModel model,
        CancellationToken cancellationToken
    )
    {
        var entity = await dbContext.ServiceGroups.SingleAsync(
            group => group.Id == id,
            cancellationToken
        );
        entity.Name = model.Name.Trim();
        entity.Slug = model.Slug.Trim();
        entity.Description = string.IsNullOrWhiteSpace(model.Description)
            ? null
            : model.Description.Trim();
        entity.DisplayOrder = model.DisplayOrder;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CreateServiceAsync(
        ServiceUpsertModel model,
        CancellationToken cancellationToken
    )
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var entity = new Service
        {
            ServiceGroupId = model.ServiceGroupId,
            Name = model.Name.Trim(),
            Slug = model.Slug.Trim(),
            Description = string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim(),
            DisplayOrder = model.DisplayOrder,
            IsEnabled = model.IsEnabled,
            CheckPeriodSeconds = model.CheckPeriodSeconds,
            FailureThreshold = model.FailureThreshold,
            RecoveryThreshold = model.RecoveryThreshold,
            RawRetentionDaysOverride = model.RawRetentionDaysOverride,
            NextCheckUtc = now,
            MonitorDefinition = new MonitorDefinition
            {
                MonitorType = model.MonitorType,
                Host = NormalizeOptionalString(model.Host),
                Port = model.MonitorType == MonitorType.Icmp ? null : model.Port,
                Url = NormalizeOptionalString(model.Url),
                HttpMethod = model.HttpMethod.Trim().ToUpperInvariant(),
                RequestHeadersJson = NormalizeOptionalString(model.RequestHeadersJson),
                RequestBody = NormalizeOptionalString(model.RequestBody),
                ExpectedStatusCodes = model.ExpectedStatusCodes.Trim(),
                ExpectedResponseSubstring = NormalizeOptionalString(
                    model.ExpectedResponseSubstring
                ),
                VerifyTlsCertificate = model.VerifyTlsCertificate,
                TimeoutSeconds = model.TimeoutSeconds,
            },
        };

        dbContext.Services.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateServiceAsync(
        Guid id,
        ServiceUpsertModel model,
        CancellationToken cancellationToken
    )
    {
        var entity = await dbContext
            .Services.Include(service => service.MonitorDefinition)
            .SingleAsync(service => service.Id == id, cancellationToken);

        entity.ServiceGroupId = model.ServiceGroupId;
        entity.Name = model.Name.Trim();
        entity.Slug = model.Slug.Trim();
        entity.Description = string.IsNullOrWhiteSpace(model.Description)
            ? null
            : model.Description.Trim();
        entity.DisplayOrder = model.DisplayOrder;
        entity.IsEnabled = model.IsEnabled;
        entity.CheckPeriodSeconds = model.CheckPeriodSeconds;
        entity.FailureThreshold = model.FailureThreshold;
        entity.RecoveryThreshold = model.RecoveryThreshold;
        entity.RawRetentionDaysOverride = model.RawRetentionDaysOverride;
        entity.MonitorDefinition.MonitorType = model.MonitorType;
        entity.MonitorDefinition.Host = NormalizeOptionalString(model.Host);
        entity.MonitorDefinition.Port =
            model.MonitorType == MonitorType.Icmp
                ? entity.MonitorDefinition.Port
                : model.Port ?? entity.MonitorDefinition.Port;
        entity.MonitorDefinition.Url = NormalizeOptionalString(model.Url);
        entity.MonitorDefinition.HttpMethod = model.HttpMethod.Trim().ToUpperInvariant();
        entity.MonitorDefinition.RequestHeadersJson = NormalizeOptionalString(
            model.RequestHeadersJson
        );
        entity.MonitorDefinition.RequestBody = NormalizeOptionalString(model.RequestBody);
        entity.MonitorDefinition.ExpectedStatusCodes = model.ExpectedStatusCodes.Trim();
        entity.MonitorDefinition.ExpectedResponseSubstring = NormalizeOptionalString(
            model.ExpectedResponseSubstring
        );
        entity.MonitorDefinition.VerifyTlsCertificate = model.VerifyTlsCertificate;
        entity.MonitorDefinition.TimeoutSeconds = model.TimeoutSeconds;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ServiceAdminModel MapService(Service service)
    {
        return new ServiceAdminModel(
            service.Id,
            service.ServiceGroupId,
            service.ServiceGroup.Name,
            service.Name,
            service.Slug,
            service.Description,
            service.IsEnabled,
            service.DisplayOrder,
            service.CheckPeriodSeconds,
            service.FailureThreshold,
            service.RecoveryThreshold,
            service.RawRetentionDaysOverride,
            service.MonitorDefinition.MonitorType,
            service.MonitorDefinition.Host,
            service.MonitorDefinition.Port,
            service.MonitorDefinition.Url,
            service.MonitorDefinition.HttpMethod,
            service.MonitorDefinition.RequestHeadersJson,
            service.MonitorDefinition.RequestBody,
            service.MonitorDefinition.ExpectedStatusCodes,
            service.MonitorDefinition.ExpectedResponseSubstring,
            service.MonitorDefinition.VerifyTlsCertificate,
            service.MonitorDefinition.TimeoutSeconds
        );
    }

    private static string? NormalizeOptionalString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
