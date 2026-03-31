using Microsoft.EntityFrameworkCore;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Domain.Entities;
using StatusPageSharp.Infrastructure.Data;

namespace StatusPageSharp.Infrastructure.Services;

public sealed class MaintenanceManagementService(ApplicationDbContext dbContext)
    : IMaintenanceManagementService
{
    public async Task<IReadOnlyList<PublicMaintenanceSummaryModel>> GetPublicMaintenanceAsync(
        CancellationToken cancellationToken
    )
    {
        var maintenanceItems = await dbContext
            .ScheduledMaintenances.AsNoTracking()
            .Include(item => item.Services)
                .ThenInclude(link => link.Service)
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

    public async Task<IReadOnlyList<MaintenanceAdminModel>> GetAdminMaintenanceAsync(
        CancellationToken cancellationToken
    )
    {
        var maintenanceItems = await dbContext
            .ScheduledMaintenances.AsNoTracking()
            .Include(item => item.Services)
                .ThenInclude(link => link.Service)
            .OrderByDescending(item => item.StartsUtc)
            .ToListAsync(cancellationToken);

        return maintenanceItems
            .Select(item => new MaintenanceAdminModel(
                item.Id,
                item.Title,
                item.Summary,
                item.StartsUtc,
                item.EndsUtc,
                item.Services.Select(link => link.ServiceId).ToList(),
                item.Services.Select(link => link.Service.Name).OrderBy(name => name).ToList()
            ))
            .ToList();
    }

    public async Task CreateMaintenanceAsync(
        MaintenanceUpsertModel model,
        string? userId,
        CancellationToken cancellationToken
    )
    {
        var maintenance = new ScheduledMaintenance
        {
            Title = model.Title.Trim(),
            Summary = model.Summary.Trim(),
            StartsUtc = ConvertLocalInputToUtc(model.StartsUtc, model.TimeZoneOffsetMinutes),
            EndsUtc = ConvertLocalInputToUtc(model.EndsUtc, model.TimeZoneOffsetMinutes),
            CreatedByUserId = userId,
            Services = model
                .ServiceIds.Select(serviceId => new ScheduledMaintenanceService
                {
                    ServiceId = serviceId,
                })
                .ToList(),
        };

        dbContext.ScheduledMaintenances.Add(maintenance);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateMaintenanceAsync(
        Guid id,
        MaintenanceUpsertModel model,
        CancellationToken cancellationToken
    )
    {
        var maintenance = await dbContext
            .ScheduledMaintenances.Include(item => item.Services)
            .SingleAsync(item => item.Id == id, cancellationToken);

        maintenance.Title = model.Title.Trim();
        maintenance.Summary = model.Summary.Trim();
        maintenance.StartsUtc = ConvertLocalInputToUtc(
            model.StartsUtc,
            model.TimeZoneOffsetMinutes
        );
        maintenance.EndsUtc = ConvertLocalInputToUtc(model.EndsUtc, model.TimeZoneOffsetMinutes);

        var desiredServiceIds = model.ServiceIds.ToHashSet();
        maintenance.Services.RemoveAll(link => !desiredServiceIds.Contains(link.ServiceId));

        var existingServiceIds = maintenance.Services.Select(link => link.ServiceId).ToHashSet();
        foreach (
            var serviceId in desiredServiceIds.Where(serviceId =>
                !existingServiceIds.Contains(serviceId)
            )
        )
        {
            dbContext.ScheduledMaintenanceServices.Add(
                new ScheduledMaintenanceService
                {
                    ScheduledMaintenance = maintenance,
                    ServiceId = serviceId,
                }
            );
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMaintenanceAsync(Guid id, CancellationToken cancellationToken)
    {
        var maintenance = await dbContext
            .ScheduledMaintenances.Include(item => item.Services)
            .SingleAsync(item => item.Id == id, cancellationToken);

        dbContext.ScheduledMaintenances.Remove(maintenance);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static DateTime ConvertLocalInputToUtc(DateTime localValue, int timeZoneOffsetMinutes)
    {
        var unspecifiedLocal = DateTime.SpecifyKind(localValue, DateTimeKind.Unspecified);
        var utcValue = unspecifiedLocal.AddMinutes(timeZoneOffsetMinutes);
        return DateTime.SpecifyKind(utcValue, DateTimeKind.Utc);
    }
}
