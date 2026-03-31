using Microsoft.EntityFrameworkCore;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Domain.Entities;
using StatusPageSharp.Domain.Enums;
using StatusPageSharp.Infrastructure.Data;

namespace StatusPageSharp.Infrastructure.Services;

public sealed class IncidentManagementService(ApplicationDbContext dbContext, IClock clock)
    : IIncidentManagementService
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

        var now = clock.UtcNow;
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

        await dbContext.SaveChangesAsync(cancellationToken);
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
        AddIncidentEvent(
            incident,
            IncidentEventType.Update,
            model.Message.Trim(),
            NormalizeOptionalText(model.Body),
            clock.UtcNow,
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
        var now = clock.UtcNow;
        var incident = await dbContext
            .Incidents.Include(item => item.AffectedServices)
                .ThenInclude(item => item.Service)
            .Include(item => item.Events)
            .SingleAsync(item => item.Id == id, cancellationToken);

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

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ResolveIncidentAsync(
        Guid id,
        string? postmortem,
        string? userId,
        CancellationToken cancellationToken
    )
    {
        var now = clock.UtcNow;
        var incident = await dbContext
            .Incidents.Include(item => item.AffectedServices)
            .Include(item => item.Events)
            .SingleAsync(item => item.Id == id, cancellationToken);

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

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private void AddAffectedService(
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
            incident
                .AffectedServices.OrderBy(item => item.Service.Name)
                .Select(item => new IncidentAffectedServiceAdminModel(
                    item.ServiceId,
                    item.Service.Name,
                    item.ImpactLevel,
                    item.AddedUtc,
                    item.IsResolved,
                    item.ResolvedUtc
                ))
                .ToList(),
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
}
