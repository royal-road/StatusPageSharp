using Microsoft.EntityFrameworkCore;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Domain.Entities;
using StatusPageSharp.Domain.Enums;
using StatusPageSharp.Domain.Logic;
using StatusPageSharp.Infrastructure.Data;

namespace StatusPageSharp.Infrastructure.Services;

public sealed class PublicStatusService(ApplicationDbContext dbContext, TimeProvider timeProvider)
    : IPublicStatusService
{
    public async Task<PublicSiteSummaryModel> GetSiteSummaryAsync(
        CancellationToken cancellationToken
    )
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var siteSetting = await GetSiteSettingAsync(cancellationToken);
        var groups = await dbContext
            .ServiceGroups.AsNoTracking()
            .Include(serviceGroup => serviceGroup.Services)
            .OrderBy(serviceGroup => serviceGroup.DisplayOrder)
            .ThenBy(serviceGroup => serviceGroup.Name)
            .ToListAsync(cancellationToken);

        var activeImpacts = await GetActiveImpactsAsync(cancellationToken);
        var maintenanceServiceIds = await GetActiveMaintenanceServiceIdsAsync(
            now,
            cancellationToken
        );
        var uptimeLookup = await GetLastThirtyDayUptimeLookupAsync(cancellationToken);
        var dailyStatusLookup = await GetDailyStatusLookupAsync(cancellationToken);

        var groupModels = groups
            .Select(group =>
            {
                var serviceModels = group
                    .Services.OrderBy(service => service.DisplayOrder)
                    .ThenBy(service => service.Name)
                    .Select(service =>
                    {
                        var serviceStatus = StatusCalculator.CalculateServiceStatus(
                            maintenanceServiceIds.Contains(service.Id),
                            activeImpacts.TryGetValue(service.Id, out var impacts) ? impacts : []
                        );

                        return new PublicServiceSummaryModel(
                            service.Id,
                            service.Name,
                            service.Slug,
                            service.Description,
                            serviceStatus,
                            service.LastLatencyMilliseconds,
                            uptimeLookup.TryGetValue(service.Id, out var uptimePercentage)
                                ? uptimePercentage
                                : null,
                            dailyStatusLookup.TryGetValue(service.Id, out var dailyStatuses)
                                ? dailyStatuses
                                : []
                        );
                    })
                    .ToList();

                var groupStatus = StatusCalculator.CalculateGroupStatus(
                    serviceModels.Select(service => service.Status).ToArray()
                );

                return new PublicServiceGroupModel(
                    group.Id,
                    group.Name,
                    group.Slug,
                    group.Description,
                    groupStatus,
                    serviceModels
                );
            })
            .ToList();

        var siteStatus = StatusCalculator.CalculateSiteStatus(
            groupModels.Select(group => group.Status).ToArray()
        );
        var activeIncidents = await GetIncidentSummariesAsync(
            IncidentStatus.Open,
            cancellationToken
        );
        var activeMaintenance = await GetPublicMaintenanceAsync(now, cancellationToken);

        return new PublicSiteSummaryModel(
            siteSetting.SiteTitle,
            siteSetting.LogoUrl,
            siteSetting.PublicRefreshIntervalSeconds,
            siteStatus,
            groupModels,
            activeIncidents,
            activeMaintenance
                .Where(maintenance => maintenance.StartsUtc <= now && maintenance.EndsUtc > now)
                .ToList()
        );
    }

    public async Task<PublicServiceDetailsModel?> GetServiceDetailsAsync(
        string slug,
        CancellationToken cancellationToken
    )
    {
        var service = await dbContext
            .Services.AsNoTracking()
            .Include(item => item.ServiceGroup)
            .SingleOrDefaultAsync(item => item.Slug == slug, cancellationToken);

        if (service is null)
        {
            return null;
        }

        var history = await BuildHistoryAsync(service, cancellationToken);
        var activeImpacts = await GetActiveImpactsAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var maintenanceServiceIds = await GetActiveMaintenanceServiceIdsAsync(
            now,
            cancellationToken
        );
        var status = StatusCalculator.CalculateServiceStatus(
            maintenanceServiceIds.Contains(service.Id),
            activeImpacts.TryGetValue(service.Id, out var impacts) ? impacts : []
        );

        return new PublicServiceDetailsModel(
            service.Id,
            service.ServiceGroupId,
            service.ServiceGroup.Name,
            service.Name,
            service.Slug,
            service.Description,
            status,
            service.LastLatencyMilliseconds,
            await GetServiceLastThirtyDayUptimeAsync(service.Id, cancellationToken),
            await GetServiceDailyStatusesAsync(service.Id, cancellationToken),
            history.UptimePoints,
            history.LatencyPoints,
            history.SlaPoints
        );
    }

    public async Task<ServiceHistorySeriesModel?> GetServiceHistoryAsync(
        string slug,
        CancellationToken cancellationToken
    )
    {
        var service = await dbContext
            .Services.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Slug == slug, cancellationToken);

        return service is null ? null : await BuildHistoryAsync(service, cancellationToken);
    }

    public async Task<IReadOnlyList<PublicIncidentSummaryModel>> GetIncidentSummariesAsync(
        CancellationToken cancellationToken
    ) => await GetIncidentSummariesAsync(null, cancellationToken);

    public async Task<PublicIncidentDetailsModel?> GetIncidentAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        var incident = await dbContext
            .Incidents.AsNoTracking()
            .Include(item => item.AffectedServices)
                .ThenInclude(item => item.Service)
            .Include(item => item.Events)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        return incident is null
            ? null
            : new PublicIncidentDetailsModel(
                incident.Id,
                incident.Title,
                incident.Summary,
                incident.StartedUtc,
                incident.ResolvedUtc,
                incident.Postmortem,
                incident
                    .AffectedServices.OrderBy(item => item.Service.Name)
                    .Select(item => new PublicIncidentServiceModel(
                        item.ServiceId,
                        item.Service.Name,
                        item.ImpactLevel,
                        item.IsResolved,
                        item.ResolvedUtc
                    ))
                    .ToList(),
                incident
                    .Events.OrderBy(item => item.CreatedUtc)
                    .Select(item => new PublicIncidentEventModel(
                        item.CreatedUtc,
                        item.Message,
                        item.Body
                    ))
                    .ToList()
            );
    }

    public async Task<IReadOnlyList<PublicMaintenanceSummaryModel>> GetPublicMaintenanceAsync(
        CancellationToken cancellationToken
    ) => await GetPublicMaintenanceAsync(timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

    private async Task<ServiceHistorySeriesModel> BuildHistoryAsync(
        Service service,
        CancellationToken cancellationToken
    )
    {
        var hourlyRollups = await dbContext
            .HourlyServiceRollups.AsNoTracking()
            .Where(rollup => rollup.ServiceId == service.Id)
            .OrderByDescending(rollup => rollup.HourStartUtc)
            .Take(48)
            .OrderBy(rollup => rollup.HourStartUtc)
            .ToListAsync(cancellationToken);

        var monthlySnapshots = await dbContext
            .MonthlySlaSnapshots.AsNoTracking()
            .Where(snapshot => snapshot.ServiceId == service.Id)
            .OrderByDescending(snapshot => snapshot.Month)
            .Take(12)
            .OrderBy(snapshot => snapshot.Month)
            .ToListAsync(cancellationToken);

        return new ServiceHistorySeriesModel(
            service.Name,
            hourlyRollups
                .Select(rollup => new HistoryPointModel(
                    rollup.HourStartUtc.ToString("MMM d, HH:mm 'UTC'"),
                    CalculateAvailabilityPercentage(
                        rollup.AvailabilitySuccessChecks,
                        rollup.AvailabilityEligibleChecks
                    )
                ))
                .ToList(),
            hourlyRollups
                .Select(rollup => new HistoryPointModel(
                    rollup.HourStartUtc.ToString("MMM d, HH:mm 'UTC'"),
                    rollup.AverageLatencyMilliseconds
                ))
                .ToList(),
            monthlySnapshots
                .Select(snapshot => new MonthlySlaPointModel(
                    snapshot.Month.ToString("MMM yyyy"),
                    snapshot.UptimePercentage
                ))
                .ToList()
        );
    }

    private async Task<SiteSetting> GetSiteSettingAsync(CancellationToken cancellationToken) =>
        await dbContext
            .SiteSettings.AsNoTracking()
            .SingleOrDefaultAsync(setting => setting.Id == 1, cancellationToken)
        ?? new SiteSetting
        {
            Id = 1,
            SiteTitle = "StatusPageSharp",
            PublicRefreshIntervalSeconds = 60,
            DefaultRawRetentionDays = 30,
        };

    private async Task<Dictionary<Guid, List<IncidentImpactLevel>>> GetActiveImpactsAsync(
        CancellationToken cancellationToken
    )
    {
        var impacts = await dbContext
            .IncidentAffectedServices.AsNoTracking()
            .Where(item => !item.IsResolved && item.Incident.Status == IncidentStatus.Open)
            .Select(item => new { item.ServiceId, item.ImpactLevel })
            .ToListAsync(cancellationToken);

        return impacts
            .GroupBy(item => item.ServiceId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => item.ImpactLevel).ToList()
            );
    }

    private async Task<HashSet<Guid>> GetActiveMaintenanceServiceIdsAsync(
        DateTime now,
        CancellationToken cancellationToken
    ) =>
        (
            await dbContext
                .ScheduledMaintenanceServices.AsNoTracking()
                .Where(link =>
                    link.ScheduledMaintenance.StartsUtc <= now
                    && link.ScheduledMaintenance.EndsUtc > now
                )
                .Select(link => link.ServiceId)
                .ToListAsync(cancellationToken)
        ).ToHashSet();

    private async Task<Dictionary<Guid, decimal?>> GetLastThirtyDayUptimeLookupAsync(
        CancellationToken cancellationToken
    )
    {
        var startDay = DateOnly.FromDateTime(
            timeProvider.GetUtcNow().UtcDateTime.Date.AddDays(-29)
        );
        var aggregates = await dbContext
            .DailyServiceRollups.AsNoTracking()
            .Where(rollup => rollup.Day >= startDay)
            .GroupBy(rollup => rollup.ServiceId)
            .Select(group => new
            {
                group.Key,
                EligibleChecks = group.Sum(item => item.AvailabilityEligibleChecks),
                SuccessfulChecks = group.Sum(item => item.AvailabilitySuccessChecks),
            })
            .ToListAsync(cancellationToken);

        return aggregates.ToDictionary(
            item => item.Key,
            item =>
                item.EligibleChecks == 0
                    ? (decimal?)null
                    : Math.Round(item.SuccessfulChecks * 100m / item.EligibleChecks, 2)
        );
    }

    private async Task<Dictionary<Guid, IReadOnlyList<DailyStatusModel>>> GetDailyStatusLookupAsync(
        CancellationToken cancellationToken
    )
    {
        var startDay = DateOnly.FromDateTime(
            timeProvider.GetUtcNow().UtcDateTime.Date.AddDays(-29)
        );
        var rollups = await dbContext
            .DailyServiceRollups.AsNoTracking()
            .Where(rollup => rollup.Day >= startDay)
            .Select(rollup => new
            {
                rollup.ServiceId,
                rollup.Day,
                rollup.AvailabilityEligibleChecks,
                rollup.AvailabilitySuccessChecks,
                rollup.IncidentCount,
            })
            .ToListAsync(cancellationToken);

        return rollups
            .GroupBy(rollup => rollup.ServiceId)
            .ToDictionary(
                group => group.Key,
                group =>
                    (IReadOnlyList<DailyStatusModel>)
                        group
                            .OrderBy(rollup => rollup.Day)
                            .Select(rollup =>
                                CreateDailyStatusModel(
                                    rollup.Day,
                                    rollup.AvailabilityEligibleChecks,
                                    rollup.AvailabilitySuccessChecks,
                                    rollup.IncidentCount
                                )
                            )
                            .ToList()
            );
    }

    private async Task<IReadOnlyList<DailyStatusModel>> GetServiceDailyStatusesAsync(
        Guid serviceId,
        CancellationToken cancellationToken
    )
    {
        var startDay = DateOnly.FromDateTime(
            timeProvider.GetUtcNow().UtcDateTime.Date.AddDays(-29)
        );
        var rollups = await dbContext
            .DailyServiceRollups.AsNoTracking()
            .Where(rollup => rollup.ServiceId == serviceId && rollup.Day >= startDay)
            .OrderBy(rollup => rollup.Day)
            .Select(rollup => new
            {
                rollup.Day,
                rollup.AvailabilityEligibleChecks,
                rollup.AvailabilitySuccessChecks,
                rollup.IncidentCount,
            })
            .ToListAsync(cancellationToken);

        return rollups
            .Select(rollup =>
                CreateDailyStatusModel(
                    rollup.Day,
                    rollup.AvailabilityEligibleChecks,
                    rollup.AvailabilitySuccessChecks,
                    rollup.IncidentCount
                )
            )
            .ToList();
    }

    private async Task<decimal?> GetServiceLastThirtyDayUptimeAsync(
        Guid serviceId,
        CancellationToken cancellationToken
    )
    {
        var startDay = DateOnly.FromDateTime(
            timeProvider.GetUtcNow().UtcDateTime.Date.AddDays(-29)
        );
        var aggregates = await dbContext
            .DailyServiceRollups.AsNoTracking()
            .Where(rollup => rollup.ServiceId == serviceId && rollup.Day >= startDay)
            .GroupBy(rollup => rollup.ServiceId)
            .Select(group => new
            {
                EligibleChecks = group.Sum(item => item.AvailabilityEligibleChecks),
                SuccessfulChecks = group.Sum(item => item.AvailabilitySuccessChecks),
            })
            .SingleOrDefaultAsync(cancellationToken);

        return aggregates is null || aggregates.EligibleChecks == 0
            ? null
            : Math.Round(aggregates.SuccessfulChecks * 100m / aggregates.EligibleChecks, 2);
    }

    private async Task<IReadOnlyList<PublicIncidentSummaryModel>> GetIncidentSummariesAsync(
        IncidentStatus? status,
        CancellationToken cancellationToken
    )
    {
        var incidents = await dbContext
            .Incidents.AsNoTracking()
            .Include(incident => incident.AffectedServices)
                .ThenInclude(item => item.Service)
            .Where(incident => status == null || incident.Status == status)
            .OrderByDescending(incident => incident.StartedUtc)
            .ToListAsync(cancellationToken);

        return incidents
            .Select(incident => new PublicIncidentSummaryModel(
                incident.Id,
                incident.Title,
                incident.Summary,
                incident.Status,
                incident.StartedUtc,
                incident.ResolvedUtc,
                incident
                    .AffectedServices.Select(item => item.Service.Name)
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList()
            ))
            .ToList();
    }

    private async Task<IReadOnlyList<PublicMaintenanceSummaryModel>> GetPublicMaintenanceAsync(
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var maintenanceItems = await dbContext
            .ScheduledMaintenances.AsNoTracking()
            .Include(item => item.Services)
                .ThenInclude(link => link.Service)
            .Where(item => item.EndsUtc >= now.AddDays(-7))
            .OrderBy(item => item.StartsUtc)
            .ToListAsync(cancellationToken);

        return maintenanceItems
            .Select(item => new PublicMaintenanceSummaryModel(
                item.Id,
                item.Title,
                item.Summary,
                item.StartsUtc,
                item.EndsUtc,
                item.Services.Select(link => link.Service.Name).OrderBy(name => name).ToList()
            ))
            .ToList();
    }

    private static decimal CalculateAvailabilityPercentage(
        int successfulChecks,
        int eligibleChecks
    ) => eligibleChecks == 0 ? 100m : Math.Round(successfulChecks * 100m / eligibleChecks, 2);

    private static DailyStatusModel CreateDailyStatusModel(
        DateOnly day,
        int eligibleChecks,
        int successfulChecks,
        int incidentCount
    ) =>
        new(
            day,
            eligibleChecks == 0 || successfulChecks == eligibleChecks,
            incidentCount > 0,
            CalculateAvailabilityPercentage(successfulChecks, eligibleChecks)
        );
}
