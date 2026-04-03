using Microsoft.EntityFrameworkCore;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Domain.Entities;
using StatusPageSharp.Domain.Enums;
using StatusPageSharp.Infrastructure.Data;

namespace StatusPageSharp.Infrastructure.Services;

public sealed class IncidentManagementService(
    ApplicationDbContext dbContext,
    TimeProvider timeProvider
) : IIncidentManagementService
{
    public async Task<IReadOnlyList<PublicIncidentSummaryModel>> GetIncidentSummariesAsync(
        CancellationToken cancellationToken
    )
    {
        var incidents = await dbContext
            .Incidents.AsNoTracking()
            .Include(incident => incident.AffectedServices)
                .ThenInclude(item => item.Service)
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
                IncidentProjectionMapper.MapPublicAffectedServices(incident.AffectedServices),
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

    public async Task<IReadOnlyList<IncidentAdminModel>> GetAdminIncidentsAsync(
        CancellationToken cancellationToken
    )
    {
        var incidents = await dbContext
            .Incidents.AsNoTracking()
            .Include(item => item.AffectedServices)
                .ThenInclude(item => item.Service)
            .Include(item => item.Events)
            .OrderByDescending(item => item.StartedUtc)
            .ToListAsync(cancellationToken);

        return incidents.Select(MapIncident).ToList();
    }

    public async Task<IncidentAdminModel?> GetAdminIncidentAsync(
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

        return incident is null ? null : MapIncident(incident);
    }

    public async Task<Guid> CreateManualIncidentAsync(
        ManualIncidentUpsertModel model,
        string? userId,
        CancellationToken cancellationToken
    )
    {
        if (model.Services.Count == 0)
        {
            throw new InvalidOperationException("At least one affected service is required.");
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
                Source = IncidentSource.Manual,
                Status = IncidentStatus.Open,
                Title = model.Title.Trim(),
                Summary = model.Summary.Trim(),
                StartedUtc = now,
                CreatedByUserId = userId,
            };

            dbContext.Incidents.Add(incident);
        }

        AddIncidentEvent(
            incident,
            incident.Events.Count == 0 ? IncidentEventType.Created : IncidentEventType.Update,
            model.Title.Trim(),
            NormalizeOptionalText(model.InitialEventBody) ?? model.Summary.Trim(),
            now,
            userId,
            false
        );

        foreach (var service in model.Services)
        {
            var existingService = incident.AffectedServices.SingleOrDefault(item =>
                item.ServiceId == service.ServiceId && !item.IsResolved
            );
            if (existingService is not null)
            {
                existingService.ImpactLevel = service.ImpactLevel;
                continue;
            }

            AddAffectedService(incident, service.ServiceId, service.ImpactLevel, now);
        }

        var serviceIdsToSync = model.Services.Select(item => item.ServiceId).ToHashSet();
        await SyncIncidentCountsAndSaveChangesAsync(
            CreateAffectedServiceSyncRanges(incident, serviceIdsToSync, DateOnly.FromDateTime(now)),
            now,
            cancellationToken
        );
        return incident.Id;
    }

    public async Task<Guid> CreateHistoricalIncidentAsync(
        HistoricalIncidentCreateModel model,
        string? userId,
        CancellationToken cancellationToken
    )
    {
        if (model.Services.Count == 0)
        {
            throw new InvalidOperationException("At least one affected service is required.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var startedUtc = NormalizeUtcInput(
            model.StartedUtc,
            "The incident start time is required."
        );
        DateTime? resolvedUtc = model.ResolvedUtc is null
            ? null
            : NormalizeUtcInput(model.ResolvedUtc.Value, "The incident resolved time is invalid.");
        var postmortem = NormalizeOptionalText(model.Postmortem);

        ValidateHistoricalIncidentWindow(startedUtc, resolvedUtc, now);
        EnsureHistoricalPostmortemIsAllowed(resolvedUtc, postmortem);
        if (resolvedUtc is null)
        {
            await EnsureNoOpenIncidentExistsAsync(cancellationToken);
        }

        var incident = new Incident
        {
            Source = IncidentSource.Manual,
            Status = resolvedUtc is null ? IncidentStatus.Open : IncidentStatus.Resolved,
            Title = model.Title.Trim(),
            Summary = model.Summary.Trim(),
            StartedUtc = startedUtc,
            ResolvedUtc = resolvedUtc,
            Postmortem = postmortem,
            CreatedByUserId = userId,
        };

        dbContext.Incidents.Add(incident);
        AddIncidentEvent(
            incident,
            IncidentEventType.Created,
            incident.Title,
            NormalizeOptionalText(model.InitialEventBody) ?? incident.Summary,
            startedUtc,
            userId,
            false
        );

        foreach (var service in model.Services)
        {
            AddAffectedService(
                incident,
                service.ServiceId,
                service.ImpactLevel,
                startedUtc,
                resolvedUtc
            );
        }

        if (resolvedUtc is not null)
        {
            AddIncidentEvent(
                incident,
                IncidentEventType.Resolved,
                "Incident resolved.",
                incident.Postmortem,
                resolvedUtc.Value,
                userId,
                false
            );
        }

        var endDay = DateOnly.FromDateTime((resolvedUtc ?? now).Date);
        await SyncIncidentCountsAndSaveChangesAsync(
            CreateServiceSyncRanges(
                model.Services.Select(item => item.ServiceId).ToHashSet(),
                DateOnly.FromDateTime(startedUtc.Date),
                endDay
            ),
            now,
            cancellationToken
        );
        return incident.Id;
    }

    public async Task AddManualEventAsync(
        Guid id,
        IncidentEventUpsertModel model,
        string? userId,
        CancellationToken cancellationToken
    )
    {
        var incident = await dbContext.Incidents.SingleAsync(
            item => item.Id == id,
            cancellationToken
        );
        EnsureIncidentCanBeModified(incident);
        AddIncidentEvent(
            incident,
            IncidentEventType.Update,
            model.Message.Trim(),
            NormalizeOptionalText(model.Body),
            timeProvider.GetUtcNow().UtcDateTime,
            userId,
            false
        );

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAffectedServicesAsync(
        Guid id,
        IncidentAffectedServicesUpdateModel model,
        string? userId,
        CancellationToken cancellationToken
    )
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var incident = await dbContext
            .Incidents.Include(item => item.AffectedServices)
                .ThenInclude(item => item.Service)
            .Include(item => item.Events)
            .SingleAsync(item => item.Id == id, cancellationToken);
        EnsureIncidentCanBeModified(incident);

        var desiredServices = model.Services.ToDictionary(
            item => item.ServiceId,
            item => item.ImpactLevel
        );
        var activeServices = incident
            .AffectedServices.Where(item => !item.IsResolved)
            .ToDictionary(item => item.ServiceId);

        foreach (
            var activeService in activeServices.Values.Where(activeService =>
                !desiredServices.ContainsKey(activeService.ServiceId)
            )
        )
        {
            activeService.IsResolved = true;
            activeService.ResolvedUtc = now;
            AddIncidentEvent(
                incident,
                IncidentEventType.ServiceRecovered,
                $"{activeService.Service.Name} marked as recovered.",
                null,
                now,
                userId,
                false
            );
        }

        foreach (var desiredService in desiredServices)
        {
            if (activeServices.TryGetValue(desiredService.Key, out var existingService))
            {
                if (existingService.ImpactLevel != desiredService.Value)
                {
                    existingService.ImpactLevel = desiredService.Value;
                    AddIncidentEvent(
                        incident,
                        IncidentEventType.Update,
                        $"{existingService.Service.Name} impact updated to {desiredService.Value}.",
                        null,
                        now,
                        userId,
                        false
                    );
                }

                continue;
            }

            var service = await dbContext.Services.SingleAsync(
                item => item.Id == desiredService.Key,
                cancellationToken
            );
            AddAffectedService(incident, service.Id, desiredService.Value, now);
            AddIncidentEvent(
                incident,
                IncidentEventType.ServiceAdded,
                $"{service.Name} added to the incident with {desiredService.Value}.",
                null,
                now,
                userId,
                false
            );
        }

        if (incident.AffectedServices.Any(item => !item.IsResolved))
        {
            incident.Status = IncidentStatus.Open;
            incident.ResolvedUtc = null;
        }
        else
        {
            incident.Status = IncidentStatus.Resolved;
            incident.ResolvedUtc = now;
            AddIncidentEvent(
                incident,
                IncidentEventType.Resolved,
                "Incident resolved.",
                null,
                now,
                userId,
                false
            );
        }

        var serviceIdsToSync = incident
            .AffectedServices.Select(item => item.ServiceId)
            .Concat(model.Services.Select(item => item.ServiceId))
            .ToHashSet();
        await SyncIncidentCountsAndSaveChangesAsync(
            CreateAffectedServiceSyncRanges(incident, serviceIdsToSync, DateOnly.FromDateTime(now)),
            now,
            cancellationToken
        );
    }

    public async Task ResolveIncidentAsync(
        Guid id,
        string? postmortem,
        string? userId,
        CancellationToken cancellationToken
    )
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var incident = await dbContext
            .Incidents.Include(item => item.AffectedServices)
            .Include(item => item.Events)
            .SingleAsync(item => item.Id == id, cancellationToken);
        EnsureIncidentCanBeModified(incident);

        foreach (var affectedService in incident.AffectedServices.Where(item => !item.IsResolved))
        {
            affectedService.IsResolved = true;
            affectedService.ResolvedUtc = now;
        }

        incident.Status = IncidentStatus.Resolved;
        incident.ResolvedUtc = now;
        incident.Postmortem = NormalizeOptionalText(postmortem);
        AddIncidentEvent(
            incident,
            IncidentEventType.Resolved,
            "Incident resolved.",
            incident.Postmortem,
            now,
            userId,
            false
        );

        var serviceIdsToSync = incident.AffectedServices.Select(item => item.ServiceId).ToHashSet();
        await SyncIncidentCountsAndSaveChangesAsync(
            CreateAffectedServiceSyncRanges(incident, serviceIdsToSync, DateOnly.FromDateTime(now)),
            now,
            cancellationToken
        );
    }

    public async Task MergeIncidentAsync(
        Guid targetId,
        Guid sourceId,
        string? userId,
        CancellationToken cancellationToken
    )
    {
        if (targetId == sourceId)
        {
            throw new InvalidOperationException("Choose a different incident to merge.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var incidents = await dbContext
            .Incidents.Include(item => item.AffectedServices)
            .Include(item => item.Events)
            .Where(item => item.Id == targetId || item.Id == sourceId)
            .ToListAsync(cancellationToken);

        var targetIncident = incidents.SingleOrDefault(item => item.Id == targetId);
        var sourceIncident = incidents.SingleOrDefault(item => item.Id == sourceId);

        if (targetIncident is null || sourceIncident is null)
        {
            throw new InvalidOperationException("The selected incident could not be found.");
        }

        var mergedAffectedServices = targetIncident
            .AffectedServices.Concat(sourceIncident.AffectedServices)
            .ToList();
        var serviceSyncRanges = CreateAffectedServiceSyncRanges(mergedAffectedServices, now);

        PreserveSummaryOnTimeline(sourceIncident);

        foreach (var affectedService in sourceIncident.AffectedServices.ToList())
        {
            sourceIncident.AffectedServices.Remove(affectedService);
            affectedService.Incident = targetIncident;
            affectedService.IncidentId = targetIncident.Id;
            targetIncident.AffectedServices.Add(affectedService);
        }

        foreach (var incidentEvent in sourceIncident.Events.ToList())
        {
            sourceIncident.Events.Remove(incidentEvent);
            incidentEvent.Incident = targetIncident;
            incidentEvent.IncidentId = targetIncident.Id;
            targetIncident.Events.Add(incidentEvent);
        }

        targetIncident.StartedUtc =
            targetIncident.StartedUtc <= sourceIncident.StartedUtc
                ? targetIncident.StartedUtc
                : sourceIncident.StartedUtc;

        var hasOpenServices = targetIncident.AffectedServices.Any(item => !item.IsResolved);
        targetIncident.Status = hasOpenServices ? IncidentStatus.Open : IncidentStatus.Resolved;
        targetIncident.ResolvedUtc = hasOpenServices
            ? null
            : GetMergedResolvedUtc(targetIncident, sourceIncident);
        targetIncident.Postmortem =
            targetIncident.Status == IncidentStatus.Resolved
                ? MergePostmortem(
                    targetIncident.Postmortem,
                    sourceIncident.Postmortem,
                    sourceIncident.Title
                )
                : null;

        if (targetIncident.Status == IncidentStatus.Resolved)
        {
            SyncResolvedEventBody(targetIncident);
        }

        dbContext.Incidents.Remove(sourceIncident);
        await SyncIncidentCountsAndSaveChangesAsync(serviceSyncRanges, now, cancellationToken);
    }

    public async Task DeleteIncidentAsync(Guid id, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var incident = await dbContext
            .Incidents.Include(item => item.AffectedServices)
            .Include(item => item.Events)
            .SingleAsync(item => item.Id == id, cancellationToken);

        var serviceIdsToSync = incident.AffectedServices.Select(item => item.ServiceId).ToHashSet();
        var serviceSyncRanges = CreateAffectedServiceSyncRanges(
            incident,
            serviceIdsToSync,
            DateOnly.FromDateTime((incident.ResolvedUtc ?? now).Date)
        );

        dbContext.IncidentEvents.RemoveRange(incident.Events);
        dbContext.IncidentAffectedServices.RemoveRange(incident.AffectedServices);
        dbContext.Incidents.Remove(incident);

        await SyncIncidentCountsAndSaveChangesAsync(serviceSyncRanges, now, cancellationToken);
    }

    public async Task UpdatePostmortemAsync(
        Guid id,
        string? postmortem,
        string? userId,
        CancellationToken cancellationToken
    )
    {
        var incident = await dbContext
            .Incidents.Include(item => item.Events)
            .SingleAsync(item => item.Id == id, cancellationToken);

        if (incident.Status != IncidentStatus.Resolved)
        {
            throw new InvalidOperationException(
                "Only resolved incidents can update the postmortem."
            );
        }

        var previousPostmortem = incident.Postmortem;
        incident.Postmortem = NormalizeOptionalText(postmortem);
        SyncResolvedEventBody(incident);

        if (
            incident.Postmortem is not null
            && !string.Equals(previousPostmortem, incident.Postmortem, StringComparison.Ordinal)
        )
        {
            AddIncidentEvent(
                incident,
                IncidentEventType.PostmortemPublished,
                "Postmortem updated.",
                null,
                timeProvider.GetUtcNow().UtcDateTime,
                userId,
                false
            );
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void AddAffectedService(
        Incident incident,
        Guid serviceId,
        IncidentImpactLevel impactLevel,
        DateTime addedUtc,
        DateTime? resolvedUtc = null
    )
    {
        dbContext.IncidentAffectedServices.Add(
            new IncidentAffectedService
            {
                Incident = incident,
                ServiceId = serviceId,
                ImpactLevel = impactLevel,
                AddedUtc = addedUtc,
                IsResolved = resolvedUtc is not null,
                ResolvedUtc = resolvedUtc,
            }
        );
    }

    private void AddIncidentEvent(
        Incident incident,
        IncidentEventType eventType,
        string message,
        string? body,
        DateTime createdUtc,
        string? userId,
        bool isSystemGenerated
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
                CreatedByUserId = userId,
                IsSystemGenerated = isSystemGenerated,
            }
        );
    }

    private static IncidentAdminModel MapIncident(Incident incident)
    {
        return new IncidentAdminModel(
            incident.Id,
            incident.Title,
            incident.Summary,
            incident.Source,
            incident.Status,
            incident.StartedUtc,
            incident.ResolvedUtc,
            incident.Postmortem,
            IncidentProjectionMapper.MapAdminAffectedServices(incident.AffectedServices),
            incident
                .Events.OrderBy(item => item.CreatedUtc)
                .Select(item => new IncidentEventAdminModel(
                    item.Id,
                    item.EventType,
                    item.Message,
                    item.Body,
                    item.IsSystemGenerated,
                    item.CreatedUtc,
                    item.CreatedByUserId
                ))
                .ToList()
        );
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? GetMergedResolvedUtc(Incident targetIncident, Incident sourceIncident)
    {
        List<DateTime> resolvedUtcValues = [];
        if (targetIncident.ResolvedUtc is not null)
        {
            resolvedUtcValues.Add(targetIncident.ResolvedUtc.Value);
        }

        if (sourceIncident.ResolvedUtc is not null)
        {
            resolvedUtcValues.Add(sourceIncident.ResolvedUtc.Value);
        }

        resolvedUtcValues.AddRange(
            targetIncident
                .AffectedServices.Where(item => item.ResolvedUtc is not null)
                .Select(item => item.ResolvedUtc!.Value)
        );

        return resolvedUtcValues.Count == 0 ? null : resolvedUtcValues.Max();
    }

    private static string? MergePostmortem(
        string? targetPostmortem,
        string? sourcePostmortem,
        string sourceTitle
    )
    {
        var normalizedTargetPostmortem = NormalizeOptionalText(targetPostmortem);
        var normalizedSourcePostmortem = NormalizeOptionalText(sourcePostmortem);

        return (normalizedTargetPostmortem, normalizedSourcePostmortem) switch
        {
            (null, null) => null,
            (not null, null) => normalizedTargetPostmortem,
            (null, not null) => normalizedSourcePostmortem,
            _ when string.Equals(
                    normalizedTargetPostmortem,
                    normalizedSourcePostmortem,
                    StringComparison.Ordinal
                ) => normalizedTargetPostmortem,
            _ =>
                $"{normalizedTargetPostmortem}\n\nMerged notes from \"{sourceTitle}\":\n{normalizedSourcePostmortem}",
        };
    }

    private static void SyncResolvedEventBody(Incident incident)
    {
        var resolvedEvent = incident
            .Events.Where(item => item.EventType == IncidentEventType.Resolved)
            .OrderByDescending(item => item.CreatedUtc)
            .FirstOrDefault();
        if (resolvedEvent is not null)
        {
            resolvedEvent.Body = incident.Postmortem;
        }
    }

    private static void PreserveSummaryOnTimeline(Incident incident)
    {
        if (string.IsNullOrWhiteSpace(incident.Summary))
        {
            return;
        }

        var createdEvent = incident
            .Events.Where(item => item.EventType == IncidentEventType.Created)
            .OrderBy(item => item.CreatedUtc)
            .FirstOrDefault();
        if (createdEvent is not null && string.IsNullOrWhiteSpace(createdEvent.Body))
        {
            createdEvent.Body = incident.Summary;
        }
    }

    private static DateTime NormalizeUtcInput(DateTime value, string missingMessage)
    {
        if (value == default)
        {
            throw new InvalidOperationException(missingMessage);
        }

        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
        };
    }

    private async Task EnsureNoOpenIncidentExistsAsync(CancellationToken cancellationToken)
    {
        var hasOpenIncident = await dbContext.Incidents.AnyAsync(
            item => item.Status == IncidentStatus.Open,
            cancellationToken
        );
        if (hasOpenIncident)
        {
            throw new InvalidOperationException(
                "A historical incident can only remain open when no other incident is currently open."
            );
        }
    }

    private static void EnsureIncidentCanBeModified(Incident incident)
    {
        if (incident.Status == IncidentStatus.Resolved)
        {
            throw new InvalidOperationException(
                "Resolved incidents can only update the postmortem."
            );
        }
    }

    private static void ValidateHistoricalIncidentWindow(
        DateTime startedUtc,
        DateTime? resolvedUtc,
        DateTime now
    )
    {
        if (startedUtc > now)
        {
            throw new InvalidOperationException("The incident start time cannot be in the future.");
        }

        if (resolvedUtc is null)
        {
            return;
        }

        if (resolvedUtc < startedUtc)
        {
            throw new InvalidOperationException(
                "The resolved time must be at or after the incident start time."
            );
        }

        if (resolvedUtc > now)
        {
            throw new InvalidOperationException("The resolved time cannot be in the future.");
        }
    }

    private static void EnsureHistoricalPostmortemIsAllowed(
        DateTime? resolvedUtc,
        string? postmortem
    )
    {
        if (resolvedUtc is null && postmortem is not null)
        {
            throw new InvalidOperationException(
                "A postmortem can only be provided for a resolved incident."
            );
        }
    }

    private static IReadOnlyDictionary<
        Guid,
        (DateOnly StartDay, DateOnly EndDay)
    > CreateServiceSyncRanges(IReadOnlySet<Guid> serviceIds, DateOnly day) =>
        CreateServiceSyncRanges(serviceIds, day, day);

    private static IReadOnlyDictionary<
        Guid,
        (DateOnly StartDay, DateOnly EndDay)
    > CreateServiceSyncRanges(IReadOnlySet<Guid> serviceIds, DateOnly startDay, DateOnly endDay)
    {
        Dictionary<Guid, (DateOnly StartDay, DateOnly EndDay)> ranges = [];
        foreach (var serviceId in serviceIds)
        {
            ranges[serviceId] = (startDay, endDay);
        }

        return ranges;
    }

    private static IReadOnlyDictionary<
        Guid,
        (DateOnly StartDay, DateOnly EndDay)
    > CreateAffectedServiceSyncRanges(
        Incident incident,
        IReadOnlySet<Guid> serviceIds,
        DateOnly endDay
    )
    {
        return CreateAffectedServiceSyncRanges(
            incident.AffectedServices.Where(item => serviceIds.Contains(item.ServiceId)),
            DateTime.SpecifyKind(endDay.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc)
        );
    }

    private static IReadOnlyDictionary<
        Guid,
        (DateOnly StartDay, DateOnly EndDay)
    > CreateAffectedServiceSyncRanges(
        IEnumerable<IncidentAffectedService> affectedServices,
        DateTime unresolvedWindowEndUtc
    )
    {
        Dictionary<Guid, (DateOnly StartDay, DateOnly EndDay)> ranges = [];
        foreach (var group in affectedServices.GroupBy(item => item.ServiceId))
        {
            var startDay = group.Min(item => DateOnly.FromDateTime(item.AddedUtc));
            var endDay = group.Max(item =>
                DateOnly.FromDateTime((item.ResolvedUtc ?? unresolvedWindowEndUtc).Date)
            );
            ranges[group.Key] = (startDay, endDay);
        }

        return ranges;
    }

    private async Task SyncIncidentCountsAndSaveChangesAsync(
        IReadOnlyDictionary<Guid, (DateOnly StartDay, DateOnly EndDay)> serviceSyncRanges,
        DateTime unresolvedWindowEndUtc,
        CancellationToken cancellationToken
    )
    {
        if (serviceSyncRanges.Count > 0)
        {
            foreach (var serviceSyncRange in serviceSyncRanges)
            {
                await DailyIncidentCountRollupSynchronizer.SyncIncidentCountsAsync(
                    dbContext,
                    serviceSyncRange.Key,
                    serviceSyncRange.Value.StartDay,
                    serviceSyncRange.Value.EndDay,
                    unresolvedWindowEndUtc,
                    cancellationToken
                );
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
