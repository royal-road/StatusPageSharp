using Microsoft.EntityFrameworkCore;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Monitoring;
using StatusPageSharp.Domain.Entities;
using StatusPageSharp.Domain.Enums;
using StatusPageSharp.Infrastructure.Data;
using StatusPageSharp.Infrastructure.Monitoring;

namespace StatusPageSharp.Infrastructure.Services;

public sealed class MonitoringCoordinator(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    MonitorProbeClient monitorProbeClient,
    TimeProvider timeProvider
) : IMonitoringCoordinator
{
    public async Task<int> ExecuteDueChecksAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var dueServiceIds = await dbContext
            .Services.Where(service =>
                service.IsEnabled && (service.NextCheckUtc == null || service.NextCheckUtc <= now)
            )
            .OrderBy(service => service.NextCheckUtc)
            .Select(service => service.Id)
            .Take(20)
            .ToListAsync(cancellationToken);

        foreach (var serviceId in dueServiceIds)
        {
            await ExecuteCheckAsync(serviceId, cancellationToken);
        }

        return dueServiceIds.Count;
    }

    public async Task RefreshDerivedDataAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var serviceIds = await dbContext
            .Services.Select(service => service.Id)
            .ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var serviceId in serviceIds)
        {
            await UpdateRollupsAsync(dbContext, serviceId, now, cancellationToken);
            await UpdateMonthlySnapshotAsync(dbContext, serviceId, now, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CleanupExpiredCheckResultsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var siteSetting =
            await dbContext.SiteSettings.SingleOrDefaultAsync(
                item => item.Id == 1,
                cancellationToken
            ) ?? new SiteSetting { Id = 1, DefaultRawRetentionDays = 30 };
        var services = await dbContext
            .Services.Select(service => new { service.Id, service.RawRetentionDaysOverride })
            .ToListAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var service in services)
        {
            var retentionDays =
                service.RawRetentionDaysOverride ?? siteSetting.DefaultRawRetentionDays;
            var cutoff = now.AddDays(-retentionDays);
            await dbContext
                .CheckResults.Where(checkResult =>
                    checkResult.ServiceId == service.Id && checkResult.StartedUtc < cutoff
                )
                .ExecuteDeleteAsync(cancellationToken);
        }
    }

    public async Task<MonitorExecutionResult> ExecuteCheckAsync(
        Guid serviceId,
        CancellationToken cancellationToken
    )
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var service = await dbContext
            .Services.Include(item => item.MonitorDefinition)
            .SingleAsync(item => item.Id == serviceId, cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        service.LastCheckStartedUtc = now;

        var isUnderMaintenance = await dbContext.ScheduledMaintenanceServices.AnyAsync(
            link =>
                link.ServiceId == service.Id
                && link.ScheduledMaintenance.StartsUtc <= now
                && link.ScheduledMaintenance.EndsUtc > now,
            cancellationToken
        );

        var probeResult = await monitorProbeClient.ExecuteAsync(
            BuildProbeRequest(service),
            cancellationToken
        );
        service.LastCheckCompletedUtc = timeProvider.GetUtcNow().UtcDateTime;
        service.LastLatencyMilliseconds = probeResult.DurationMilliseconds;
        service.LastFailureKind = probeResult.FailureKind;
        service.LastFailureMessage = probeResult.FailureMessage;
        service.NextCheckUtc = now.AddSeconds(service.CheckPeriodSeconds);

        var checkResult = new CheckResult
        {
            ServiceId = service.Id,
            StartedUtc = now,
            CompletedUtc = service.LastCheckCompletedUtc.Value,
            DurationMilliseconds = probeResult.DurationMilliseconds,
            IsSuccess = probeResult.IsSuccess,
            IsIgnoredForAvailability = isUnderMaintenance,
            HttpStatusCode = probeResult.HttpStatusCode,
            FailureKind = probeResult.FailureKind,
            FailureMessage = probeResult.FailureMessage,
            ResponseSnippet = probeResult.ResponseSnippet,
        };

        dbContext.CheckResults.Add(checkResult);

        if (!isUnderMaintenance)
        {
            if (probeResult.IsSuccess)
            {
                service.ConsecutiveSuccessCount += 1;
                service.ConsecutiveFailureCount = 0;
                await ResolveServiceIncidentIfNeededAsync(dbContext, service, cancellationToken);
            }
            else
            {
                service.ConsecutiveFailureCount += 1;
                service.ConsecutiveSuccessCount = 0;
                await OpenOrUpdateIncidentIfNeededAsync(dbContext, service, cancellationToken);
            }
        }

        await UpdateRollupsAsync(
            dbContext,
            service.Id,
            service.LastCheckCompletedUtc.Value,
            cancellationToken
        );
        await UpdateMonthlySnapshotAsync(
            dbContext,
            service.Id,
            service.LastCheckCompletedUtc.Value,
            cancellationToken
        );
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MonitorExecutionResult(
            service.Id,
            service.Name,
            probeResult.IsSuccess,
            isUnderMaintenance,
            probeResult.DurationMilliseconds,
            probeResult.FailureKind,
            probeResult.FailureMessage
        );
    }

    private static CheckProbeRequest BuildProbeRequest(Service service)
    {
        return new CheckProbeRequest(
            service.Id,
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

    private async Task OpenOrUpdateIncidentIfNeededAsync(
        ApplicationDbContext dbContext,
        Service service,
        CancellationToken cancellationToken
    )
    {
        if (service.ConsecutiveFailureCount < service.FailureThreshold)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var incident = await dbContext
            .Incidents.Include(item => item.AffectedServices)
                .ThenInclude(item => item.Service)
            .Include(item => item.Events)
            .Where(item => item.Status == IncidentStatus.Open)
            .OrderBy(item => item.StartedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (incident is null)
        {
            incident = new Incident
            {
                Source = IncidentSource.Automatic,
                Status = IncidentStatus.Open,
                Title = $"{service.Name} is experiencing a major outage",
                Summary =
                    service.LastFailureMessage
                    ?? "The service stopped responding to health checks.",
                StartedUtc = now,
            };

            dbContext.Incidents.Add(incident);
            AddAffectedService(
                dbContext,
                incident,
                service.Id,
                IncidentImpactLevel.MajorOutage,
                now
            );
            AddIncidentEvent(
                dbContext,
                incident,
                IncidentEventType.Created,
                $"{service.Name} crossed its failure threshold and opened an automatic incident.",
                service.LastFailureMessage,
                now
            );
            return;
        }

        var existingAffectedService = incident.AffectedServices.SingleOrDefault(item =>
            item.ServiceId == service.Id && !item.IsResolved
        );
        if (existingAffectedService is not null)
        {
            return;
        }

        AddAffectedService(dbContext, incident, service.Id, IncidentImpactLevel.MajorOutage, now);
        AddIncidentEvent(
            dbContext,
            incident,
            IncidentEventType.ServiceAdded,
            $"{service.Name} joined the ongoing incident after additional failures.",
            service.LastFailureMessage,
            now
        );
    }

    private async Task ResolveServiceIncidentIfNeededAsync(
        ApplicationDbContext dbContext,
        Service service,
        CancellationToken cancellationToken
    )
    {
        if (service.ConsecutiveSuccessCount < service.RecoveryThreshold)
        {
            return;
        }

        var incident = await dbContext
            .Incidents.Include(item => item.AffectedServices)
                .ThenInclude(item => item.Service)
            .Include(item => item.Events)
            .Where(item => item.Status == IncidentStatus.Open)
            .OrderBy(item => item.StartedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (incident is null)
        {
            return;
        }

        var affectedService = incident.AffectedServices.SingleOrDefault(item =>
            item.ServiceId == service.Id && !item.IsResolved
        );
        if (affectedService is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        affectedService.IsResolved = true;
        affectedService.ResolvedUtc = now;

        AddIncidentEvent(
            dbContext,
            incident,
            IncidentEventType.ServiceRecovered,
            $"{service.Name} reached its recovery threshold.",
            null,
            now
        );

        if (incident.AffectedServices.All(item => item.IsResolved))
        {
            incident.Status = IncidentStatus.Resolved;
            incident.ResolvedUtc = now.AddSeconds(1);
            AddIncidentEvent(
                dbContext,
                incident,
                IncidentEventType.Resolved,
                "The incident resolved automatically after all affected services recovered.",
                null,
                now
            );
        }
    }

    private static void AddAffectedService(
        ApplicationDbContext dbContext,
        Incident incident,
        Guid serviceId,
        IncidentImpactLevel impactLevel,
        DateTime addedUtc
    )
    {
        dbContext.IncidentAffectedServices.Add(
            new IncidentAffectedService
            {
                Incident = incident,
                ServiceId = serviceId,
                ImpactLevel = impactLevel,
                AddedUtc = addedUtc,
            }
        );
    }

    private static void AddIncidentEvent(
        ApplicationDbContext dbContext,
        Incident incident,
        IncidentEventType eventType,
        string message,
        string? body,
        DateTime createdUtc
    )
    {
        dbContext.IncidentEvents.Add(
            new IncidentEvent
            {
                Incident = incident,
                EventType = eventType,
                Message = message,
                Body = body,
                CreatedUtc = createdUtc,
                IsSystemGenerated = true,
            }
        );
    }

    private async Task UpdateRollupsAsync(
        ApplicationDbContext dbContext,
        Guid serviceId,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var service = await dbContext
            .Services.AsNoTracking()
            .SingleAsync(item => item.Id == serviceId, cancellationToken);
        var hourStart = new DateTime(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            0,
            0,
            DateTimeKind.Utc
        );
        var day = DateOnly.FromDateTime(now);
        var dayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var checkUnitMinutes = Math.Max(1, (int)Math.Ceiling(service.CheckPeriodSeconds / 60m));

        var hourChecks = await dbContext
            .CheckResults.AsNoTracking()
            .Where(item =>
                item.ServiceId == serviceId
                && item.StartedUtc >= hourStart
                && item.StartedUtc < hourStart.AddHours(1)
            )
            .OrderBy(item => item.DurationMilliseconds)
            .ToListAsync(cancellationToken);

        var hourlyRollup = await dbContext.HourlyServiceRollups.SingleOrDefaultAsync(
            item => item.ServiceId == serviceId && item.HourStartUtc == hourStart,
            cancellationToken
        );

        hourlyRollup ??= new HourlyServiceRollup
        {
            ServiceId = serviceId,
            HourStartUtc = hourStart,
        };

        ApplyRollup(hourlyRollup, hourChecks, checkUnitMinutes);
        if (dbContext.Entry(hourlyRollup).State == EntityState.Detached)
        {
            dbContext.HourlyServiceRollups.Add(hourlyRollup);
        }

        var dayChecks = await dbContext
            .CheckResults.AsNoTracking()
            .Where(item =>
                item.ServiceId == serviceId
                && item.StartedUtc >= dayStart
                && item.StartedUtc < dayStart.AddDays(1)
            )
            .OrderBy(item => item.DurationMilliseconds)
            .ToListAsync(cancellationToken);

        var dailyRollup = await dbContext.DailyServiceRollups.SingleOrDefaultAsync(
            item => item.ServiceId == serviceId && item.Day == day,
            cancellationToken
        );

        dailyRollup ??= new DailyServiceRollup { ServiceId = serviceId, Day = day };

        ApplyRollup(dailyRollup, dayChecks, checkUnitMinutes);
        if (dbContext.Entry(dailyRollup).State == EntityState.Detached)
        {
            dbContext.DailyServiceRollups.Add(dailyRollup);
        }

        await DailyIncidentCountRollupSynchronizer.SyncRecentIncidentCountsAsync(
            dbContext,
            serviceId,
            now,
            cancellationToken
        );
    }

    private async Task UpdateMonthlySnapshotAsync(
        ApplicationDbContext dbContext,
        Guid serviceId,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var service = await dbContext
            .Services.AsNoTracking()
            .SingleAsync(item => item.Id == serviceId, cancellationToken);
        var month = new DateOnly(now.Year, now.Month, 1);
        var dailyRollups = await dbContext
            .DailyServiceRollups.AsNoTracking()
            .Where(item =>
                item.ServiceId == serviceId && item.Day >= month && item.Day < month.AddMonths(1)
            )
            .ToListAsync(cancellationToken);

        var eligibleChecks = dailyRollups.Sum(item => item.AvailabilityEligibleChecks);
        var successChecks = dailyRollups.Sum(item => item.AvailabilitySuccessChecks);
        var checkUnitMinutes = Math.Max(1, (int)Math.Ceiling(service.CheckPeriodSeconds / 60m));
        var eligibleMinutes = eligibleChecks * checkUnitMinutes;
        var downtimeMinutes = Math.Max(0, (eligibleChecks - successChecks) * checkUnitMinutes);
        var maintenanceMinutes = dailyRollups.Sum(item => item.MaintenanceMinutes);
        var uptimePercentage =
            eligibleChecks == 0 ? 100m : Math.Round(successChecks * 100m / eligibleChecks, 2);

        var snapshot = await dbContext.MonthlySlaSnapshots.SingleOrDefaultAsync(
            item => item.ServiceId == serviceId && item.Month == month,
            cancellationToken
        );

        snapshot ??= new MonthlySlaSnapshot { ServiceId = serviceId, Month = month };

        snapshot.EligibleMinutes = eligibleMinutes;
        snapshot.DowntimeMinutes = downtimeMinutes;
        snapshot.MaintenanceMinutes = maintenanceMinutes;
        snapshot.UptimePercentage = uptimePercentage;
        snapshot.ComputedUtc = now;

        if (dbContext.Entry(snapshot).State == EntityState.Detached)
        {
            dbContext.MonthlySlaSnapshots.Add(snapshot);
        }
    }

    private void ApplyRollup(
        HourlyServiceRollup rollup,
        IReadOnlyList<CheckResult> checkResults,
        int checkUnitMinutes
    )
    {
        rollup.TotalChecks = checkResults.Count;
        rollup.SuccessfulChecks = checkResults.Count(checkResult => checkResult.IsSuccess);
        rollup.AvailabilityEligibleChecks = checkResults.Count(checkResult =>
            !checkResult.IsIgnoredForAvailability
        );
        rollup.AvailabilitySuccessChecks = checkResults.Count(checkResult =>
            !checkResult.IsIgnoredForAvailability && checkResult.IsSuccess
        );
        rollup.TotalLatencyMilliseconds = checkResults.Sum(checkResult =>
            checkResult.DurationMilliseconds
        );
        rollup.AverageLatencyMilliseconds =
            checkResults.Count == 0
                ? 0
                : (int)
                    Math.Round(
                        checkResults.Average(checkResult => checkResult.DurationMilliseconds)
                    );
        rollup.P95LatencyMilliseconds = CalculateP95(
            checkResults.Select(checkResult => checkResult.DurationMilliseconds).ToArray()
        );
        rollup.DowntimeMinutes =
            checkResults.Count(checkResult =>
                !checkResult.IsIgnoredForAvailability && !checkResult.IsSuccess
            ) * checkUnitMinutes;
        rollup.MaintenanceMinutes =
            checkResults.Count(checkResult => checkResult.IsIgnoredForAvailability)
            * checkUnitMinutes;
    }

    private void ApplyRollup(
        DailyServiceRollup rollup,
        IReadOnlyList<CheckResult> checkResults,
        int checkUnitMinutes
    )
    {
        rollup.TotalChecks = checkResults.Count;
        rollup.SuccessfulChecks = checkResults.Count(checkResult => checkResult.IsSuccess);
        rollup.AvailabilityEligibleChecks = checkResults.Count(checkResult =>
            !checkResult.IsIgnoredForAvailability
        );
        rollup.AvailabilitySuccessChecks = checkResults.Count(checkResult =>
            !checkResult.IsIgnoredForAvailability && checkResult.IsSuccess
        );
        rollup.TotalLatencyMilliseconds = checkResults.Sum(checkResult =>
            checkResult.DurationMilliseconds
        );
        rollup.AverageLatencyMilliseconds =
            checkResults.Count == 0
                ? 0
                : (int)
                    Math.Round(
                        checkResults.Average(checkResult => checkResult.DurationMilliseconds)
                    );
        rollup.P95LatencyMilliseconds = CalculateP95(
            checkResults.Select(checkResult => checkResult.DurationMilliseconds).ToArray()
        );
        rollup.DowntimeMinutes =
            checkResults.Count(checkResult =>
                !checkResult.IsIgnoredForAvailability && !checkResult.IsSuccess
            ) * checkUnitMinutes;
        rollup.MaintenanceMinutes =
            checkResults.Count(checkResult => checkResult.IsIgnoredForAvailability)
            * checkUnitMinutes;
    }

    private static int CalculateP95(IReadOnlyList<int> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        var orderedValues = values.Order().ToArray();
        var index = (int)Math.Ceiling(orderedValues.Length * 0.95m) - 1;
        return orderedValues[Math.Clamp(index, 0, orderedValues.Length - 1)];
    }
}
