using Microsoft.EntityFrameworkCore;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Domain.Entities;
using StatusPageSharp.Infrastructure.Data;
using StatusPageSharp.Infrastructure.Services;

namespace StatusPageSharp.Infrastructure.Tests;

public class MaintenanceManagementServiceTests
{
    [Fact]
    public async Task CreateMaintenanceAsync_StoresUtcTimestamps()
    {
        await using var dbContext = CreateDbContext();
        var service = new MaintenanceManagementService(dbContext);
        var model = new MaintenanceUpsertModel
        {
            Title = "Scheduled work",
            Summary = "Deploying updates",
            StartsUtc = new DateTime(2026, 3, 30, 10, 0, 0),
            EndsUtc = new DateTime(2026, 3, 30, 11, 30, 0),
            TimeZoneOffsetMinutes = 240,
        };

        await service.CreateMaintenanceAsync(model, null, CancellationToken.None);

        var maintenance = await dbContext.ScheduledMaintenances.SingleAsync();
        Assert.Equal(new DateTime(2026, 3, 30, 14, 0, 0, DateTimeKind.Utc), maintenance.StartsUtc);
        Assert.Equal(new DateTime(2026, 3, 30, 15, 30, 0, DateTimeKind.Utc), maintenance.EndsUtc);
        Assert.Equal(DateTimeKind.Utc, maintenance.StartsUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, maintenance.EndsUtc.Kind);
    }

    [Fact]
    public async Task UpdateMaintenanceAsync_StoresUtcTimestamps()
    {
        await using var dbContext = CreateDbContext();
        var maintenance = new ScheduledMaintenance
        {
            Title = "Existing work",
            Summary = "Original window",
            StartsUtc = new DateTime(2026, 3, 30, 12, 0, 0, DateTimeKind.Utc),
            EndsUtc = new DateTime(2026, 3, 30, 13, 0, 0, DateTimeKind.Utc),
        };

        dbContext.ScheduledMaintenances.Add(maintenance);
        await dbContext.SaveChangesAsync();

        var service = new MaintenanceManagementService(dbContext);
        var model = new MaintenanceUpsertModel
        {
            Title = "Existing work",
            Summary = "Adjusted window",
            StartsUtc = new DateTime(2026, 3, 30, 9, 15, 0),
            EndsUtc = new DateTime(2026, 3, 30, 10, 45, 0),
            TimeZoneOffsetMinutes = 240,
        };

        await service.UpdateMaintenanceAsync(maintenance.Id, model, CancellationToken.None);

        var updatedMaintenance = await dbContext.ScheduledMaintenances.SingleAsync();
        Assert.Equal(
            new DateTime(2026, 3, 30, 13, 15, 0, DateTimeKind.Utc),
            updatedMaintenance.StartsUtc
        );
        Assert.Equal(
            new DateTime(2026, 3, 30, 14, 45, 0, DateTimeKind.Utc),
            updatedMaintenance.EndsUtc
        );
        Assert.Equal(DateTimeKind.Utc, updatedMaintenance.StartsUtc.Kind);
        Assert.Equal(DateTimeKind.Utc, updatedMaintenance.EndsUtc.Kind);
    }

    [Fact]
    public async Task DeleteMaintenanceAsync_RemovesMaintenanceAndServiceLinks()
    {
        await using var dbContext = CreateDbContext();
        var monitoredService = new Service
        {
            Name = "API",
            Slug = "api",
            Description = "Primary API",
            IsEnabled = true,
            CheckPeriodSeconds = 60,
        };

        var maintenance = new ScheduledMaintenance
        {
            Title = "Existing work",
            Summary = "Rolling deploy",
            StartsUtc = new DateTime(2026, 3, 30, 12, 0, 0, DateTimeKind.Utc),
            EndsUtc = new DateTime(2026, 3, 30, 13, 0, 0, DateTimeKind.Utc),
            Services = [new ScheduledMaintenanceService { Service = monitoredService }],
        };

        dbContext.ScheduledMaintenances.Add(maintenance);
        await dbContext.SaveChangesAsync();

        var service = new MaintenanceManagementService(dbContext);

        await service.DeleteMaintenanceAsync(maintenance.Id, CancellationToken.None);

        Assert.Empty(await dbContext.ScheduledMaintenances.ToListAsync());
        Assert.Empty(await dbContext.ScheduledMaintenanceServices.ToListAsync());
    }

    [Fact]
    public async Task UpdateMaintenanceAsync_AddsNewServiceLinksWithoutConcurrencyFailure()
    {
        await using var dbContext = CreateDbContext();
        var existingService = new Service
        {
            Name = "API",
            Slug = "api",
            Description = "Primary API",
            IsEnabled = true,
            CheckPeriodSeconds = 60,
        };
        var addedService = new Service
        {
            Name = "Web",
            Slug = "web",
            Description = "Public site",
            IsEnabled = true,
            CheckPeriodSeconds = 60,
        };
        var maintenance = new ScheduledMaintenance
        {
            Title = "Existing work",
            Summary = "Rolling deploy",
            StartsUtc = new DateTime(2026, 3, 30, 12, 0, 0, DateTimeKind.Utc),
            EndsUtc = new DateTime(2026, 3, 30, 13, 0, 0, DateTimeKind.Utc),
            Services = [new ScheduledMaintenanceService { Service = existingService }],
        };

        dbContext.ScheduledMaintenances.Add(maintenance);
        dbContext.Services.Add(addedService);
        await dbContext.SaveChangesAsync();

        var service = new MaintenanceManagementService(dbContext);
        var model = new MaintenanceUpsertModel
        {
            Title = maintenance.Title,
            Summary = maintenance.Summary,
            StartsUtc = new DateTime(2026, 3, 30, 8, 0, 0),
            EndsUtc = new DateTime(2026, 3, 30, 9, 0, 0),
            TimeZoneOffsetMinutes = 240,
            ServiceIds = [existingService.Id, addedService.Id],
        };

        await service.UpdateMaintenanceAsync(maintenance.Id, model, CancellationToken.None);

        var links = await dbContext
            .ScheduledMaintenanceServices.Where(link =>
                link.ScheduledMaintenanceId == maintenance.Id
            )
            .OrderBy(link => link.ServiceId)
            .ToListAsync();
        Guid[] expectedServiceIds = [existingService.Id, addedService.Id];
        Array.Sort(expectedServiceIds);

        Assert.Equal(2, links.Count);
        Assert.Equal(expectedServiceIds, links.Select(link => link.ServiceId).ToArray());
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
