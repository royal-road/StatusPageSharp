using Microsoft.EntityFrameworkCore;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Domain.Entities;
using StatusPageSharp.Domain.Enums;
using StatusPageSharp.Infrastructure.Data;
using StatusPageSharp.Infrastructure.Services;
using StatusPageSharp.Infrastructure.Tests.Support;

namespace StatusPageSharp.Infrastructure.Tests;

public class IncidentManagementServiceTests
{
    [Fact]
    public async Task CreateManualIncidentAsync_SyncsIncidentCountIntoRecentDailyRollups()
    {
        var now = new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc);
        var options = CreateOptions();
        Guid serviceId;

        await using (var seedContext = new ApplicationDbContext(options))
        {
            var service = CreateService("API", "api");
            serviceId = service.Id;
            seedContext.Services.Add(service);
            await seedContext.SaveChangesAsync();
        }

        await using (var actionContext = new ApplicationDbContext(options))
        {
            var serviceUnderTest = new IncidentManagementService(
                actionContext,
                new TestTimeProvider(now)
            );

            await serviceUnderTest.CreateManualIncidentAsync(
                new ManualIncidentUpsertModel
                {
                    Title = "API incident",
                    Summary = "Investigating elevated error rates.",
                    Services =
                    [
                        new IncidentAffectedServiceInputModel
                        {
                            ServiceId = serviceId,
                            ImpactLevel = IncidentImpactLevel.PartialOutage,
                        },
                    ],
                },
                null,
                CancellationToken.None
            );
        }

        await using var verificationContext = new ApplicationDbContext(options);
        var incidentRollup = await verificationContext.DailyServiceRollups.SingleAsync(item =>
            item.ServiceId == serviceId && item.Day == DateOnly.FromDateTime(now)
        );

        Assert.Equal(1, incidentRollup.IncidentCount);
    }

    [Fact]
    public async Task ResolveIncidentAsync_SyncsIncidentCountIntoRecentDailyRollups()
    {
        var now = new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc);
        var incidentStartedUtc = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        var options = CreateOptions();
        Guid incidentId;
        Guid serviceId;

        await using (var seedContext = new ApplicationDbContext(options))
        {
            var service = CreateService("API", "api");
            serviceId = service.Id;
            seedContext.Services.Add(service);
            seedContext.Incidents.Add(
                new Incident
                {
                    Source = IncidentSource.Manual,
                    Status = IncidentStatus.Open,
                    Title = "API incident",
                    Summary = "Investigating elevated error rates.",
                    StartedUtc = incidentStartedUtc,
                    AffectedServices =
                    [
                        new IncidentAffectedService
                        {
                            Service = service,
                            ImpactLevel = IncidentImpactLevel.PartialOutage,
                            AddedUtc = incidentStartedUtc,
                        },
                    ],
                }
            );

            await seedContext.SaveChangesAsync();
            incidentId = await seedContext.Incidents.Select(item => item.Id).SingleAsync();
        }

        await using (var actionContext = new ApplicationDbContext(options))
        {
            var serviceUnderTest = new IncidentManagementService(
                actionContext,
                new TestTimeProvider(now)
            );

            await serviceUnderTest.ResolveIncidentAsync(
                incidentId,
                null,
                null,
                CancellationToken.None
            );
        }

        await using var verificationContext = new ApplicationDbContext(options);
        var incidentRollups = await verificationContext
            .DailyServiceRollups.Where(item => item.ServiceId == serviceId)
            .OrderBy(item => item.Day)
            .ToListAsync();

        Assert.Equal(
            [DateOnly.FromDateTime(incidentStartedUtc), DateOnly.FromDateTime(now)],
            incidentRollups.Select(item => item.Day)
        );
        Assert.All(incidentRollups, item => Assert.Equal(1, item.IncidentCount));
    }

    [Fact]
    public async Task CreateHistoricalIncidentAsync_UsesProvidedUtcTimestamps()
    {
        var now = new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc);
        var options = CreateOptions();
        Guid serviceId;

        await using (var seedContext = new ApplicationDbContext(options))
        {
            var service = CreateService("API", "api");
            serviceId = service.Id;
            seedContext.Services.Add(service);
            await seedContext.SaveChangesAsync();
        }

        await using (var actionContext = new ApplicationDbContext(options))
        {
            var serviceUnderTest = new IncidentManagementService(
                actionContext,
                new TestTimeProvider(now)
            );

            await serviceUnderTest.CreateHistoricalIncidentAsync(
                new HistoricalIncidentCreateModel
                {
                    Title = "Historical API incident",
                    Summary = "Recovered after investigation.",
                    StartedUtc = new DateTime(2026, 1, 10, 13, 0, 0, DateTimeKind.Utc),
                    ResolvedUtc = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc),
                    Services =
                    [
                        new IncidentAffectedServiceInputModel
                        {
                            ServiceId = serviceId,
                            ImpactLevel = IncidentImpactLevel.PartialOutage,
                        },
                    ],
                },
                null,
                CancellationToken.None
            );
        }

        await using var verificationContext = new ApplicationDbContext(options);
        var incident = await verificationContext
            .Incidents.Include(item => item.AffectedServices)
            .Include(item => item.Events)
            .SingleAsync();

        Assert.Equal(new DateTime(2026, 1, 10, 13, 0, 0, DateTimeKind.Utc), incident.StartedUtc);
        Assert.Equal(new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc), incident.ResolvedUtc);

        var affectedService = Assert.Single(incident.AffectedServices);
        Assert.Equal(incident.StartedUtc, affectedService.AddedUtc);
        Assert.Equal(incident.ResolvedUtc, affectedService.ResolvedUtc);

        var resolvedEvent = Assert.Single(
            incident.Events,
            item => item.EventType == IncidentEventType.Resolved
        );
        Assert.Equal(incident.ResolvedUtc, resolvedEvent.CreatedUtc);
    }

    [Fact]
    public async Task CreateHistoricalIncidentAsync_Throws_WhenOpenIncidentIncludesPostmortem()
    {
        var now = new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc);
        var options = CreateOptions();
        Guid serviceId;

        await using (var seedContext = new ApplicationDbContext(options))
        {
            var service = CreateService("API", "api");
            serviceId = service.Id;
            seedContext.Services.Add(service);
            await seedContext.SaveChangesAsync();
        }

        await using (var actionContext = new ApplicationDbContext(options))
        {
            var serviceUnderTest = new IncidentManagementService(
                actionContext,
                new TestTimeProvider(now)
            );

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                serviceUnderTest.CreateHistoricalIncidentAsync(
                    new HistoricalIncidentCreateModel
                    {
                        Title = "Historical API incident",
                        Summary = "Recovered after investigation.",
                        StartedUtc = new DateTime(2026, 1, 10, 13, 0, 0, DateTimeKind.Utc),
                        Postmortem = "Final write-up",
                        Services =
                        [
                            new IncidentAffectedServiceInputModel
                            {
                                ServiceId = serviceId,
                                ImpactLevel = IncidentImpactLevel.PartialOutage,
                            },
                        ],
                    },
                    null,
                    CancellationToken.None
                )
            );

            Assert.Equal(
                "A postmortem can only be provided for a resolved incident.",
                exception.Message
            );
        }
    }

    [Fact]
    public async Task DeleteIncidentAsync_RemovesIncidentDataAndSyncsDailyRollups()
    {
        var now = new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc);
        var incidentStartedUtc = new DateTime(2026, 4, 2, 10, 0, 0, DateTimeKind.Utc);
        var options = CreateOptions();
        Guid incidentId;
        Guid serviceId;

        await using (var seedContext = new ApplicationDbContext(options))
        {
            var service = CreateService("API", "api");
            serviceId = service.Id;

            var incident = new Incident
            {
                Source = IncidentSource.Automatic,
                Status = IncidentStatus.Open,
                Title = "API incident",
                Summary = "Triggered by a bad configuration.",
                StartedUtc = incidentStartedUtc,
                AffectedServices =
                [
                    new IncidentAffectedService
                    {
                        Service = service,
                        ImpactLevel = IncidentImpactLevel.PartialOutage,
                        AddedUtc = incidentStartedUtc,
                    },
                ],
                Events =
                [
                    new IncidentEvent
                    {
                        EventType = IncidentEventType.Created,
                        Message = "Incident opened",
                        CreatedUtc = incidentStartedUtc,
                    },
                ],
            };

            seedContext.Incidents.Add(incident);
            seedContext.DailyServiceRollups.AddRange(
                new DailyServiceRollup
                {
                    Service = service,
                    Day = DateOnly.FromDateTime(incidentStartedUtc),
                    IncidentCount = 1,
                },
                new DailyServiceRollup
                {
                    Service = service,
                    Day = DateOnly.FromDateTime(now),
                    IncidentCount = 1,
                }
            );

            await seedContext.SaveChangesAsync();
            incidentId = incident.Id;
        }

        await using (var actionContext = new ApplicationDbContext(options))
        {
            var serviceUnderTest = new IncidentManagementService(
                actionContext,
                new TestTimeProvider(now)
            );

            await serviceUnderTest.DeleteIncidentAsync(incidentId, CancellationToken.None);
        }

        await using var verificationContext = new ApplicationDbContext(options);
        Assert.Empty(await verificationContext.Incidents.ToListAsync());
        Assert.Empty(await verificationContext.IncidentAffectedServices.ToListAsync());
        Assert.Empty(await verificationContext.IncidentEvents.ToListAsync());

        var incidentRollups = await verificationContext
            .DailyServiceRollups.Where(item => item.ServiceId == serviceId)
            .OrderBy(item => item.Day)
            .ToListAsync();

        Assert.Equal(
            [DateOnly.FromDateTime(incidentStartedUtc), DateOnly.FromDateTime(now)],
            incidentRollups.Select(item => item.Day)
        );
        Assert.All(incidentRollups, item => Assert.Equal(0, item.IncidentCount));
    }

    [Fact]
    public async Task UpdatePostmortemAsync_SynchronizesResolvedEventBody()
    {
        var now = new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc);
        var options = CreateOptions();
        Guid incidentId;

        await using (var seedContext = new ApplicationDbContext(options))
        {
            var seedIncident = new Incident
            {
                Source = IncidentSource.Manual,
                Status = IncidentStatus.Resolved,
                Title = "Resolved API incident",
                Summary = "Recovered",
                StartedUtc = now.AddHours(-2),
                ResolvedUtc = now.AddHours(-1),
                Postmortem = "Initial postmortem",
                Events =
                [
                    new IncidentEvent
                    {
                        EventType = IncidentEventType.Created,
                        Message = "Incident opened",
                        CreatedUtc = now.AddHours(-2),
                    },
                    new IncidentEvent
                    {
                        EventType = IncidentEventType.Resolved,
                        Message = "Incident resolved.",
                        Body = "Initial postmortem",
                        CreatedUtc = now.AddHours(-1),
                    },
                ],
            };

            seedContext.Incidents.Add(seedIncident);
            await seedContext.SaveChangesAsync();
            incidentId = seedIncident.Id;
        }

        await using (var actionContext = new ApplicationDbContext(options))
        {
            var serviceUnderTest = new IncidentManagementService(
                actionContext,
                new TestTimeProvider(now)
            );

            await serviceUnderTest.UpdatePostmortemAsync(
                incidentId,
                "Updated postmortem",
                null,
                CancellationToken.None
            );
        }

        await using var verificationContext = new ApplicationDbContext(options);
        var incident = await verificationContext
            .Incidents.Include(item => item.Events)
            .SingleAsync(item => item.Id == incidentId);

        Assert.Equal("Updated postmortem", incident.Postmortem);

        var resolvedEvent = Assert.Single(
            incident.Events,
            item => item.EventType == IncidentEventType.Resolved
        );
        Assert.Equal("Updated postmortem", resolvedEvent.Body);

        var postmortemEvent = Assert.Single(
            incident.Events,
            item => item.EventType == IncidentEventType.PostmortemPublished
        );
        Assert.Null(postmortemEvent.Body);
    }

    [Fact]
    public async Task UpdatePostmortemAsync_DoesNotPublishEvent_WhenPostmortemIsCleared()
    {
        var now = new DateTime(2026, 4, 3, 12, 0, 0, DateTimeKind.Utc);
        var options = CreateOptions();
        Guid incidentId;

        await using (var seedContext = new ApplicationDbContext(options))
        {
            var seedIncident = new Incident
            {
                Source = IncidentSource.Manual,
                Status = IncidentStatus.Resolved,
                Title = "Resolved API incident",
                Summary = "Recovered",
                StartedUtc = now.AddHours(-2),
                ResolvedUtc = now.AddHours(-1),
                Postmortem = "Initial postmortem",
                Events =
                [
                    new IncidentEvent
                    {
                        EventType = IncidentEventType.Created,
                        Message = "Incident opened",
                        CreatedUtc = now.AddHours(-2),
                    },
                    new IncidentEvent
                    {
                        EventType = IncidentEventType.Resolved,
                        Message = "Incident resolved.",
                        Body = "Initial postmortem",
                        CreatedUtc = now.AddHours(-1),
                    },
                ],
            };

            seedContext.Incidents.Add(seedIncident);
            await seedContext.SaveChangesAsync();
            incidentId = seedIncident.Id;
        }

        await using (var actionContext = new ApplicationDbContext(options))
        {
            var serviceUnderTest = new IncidentManagementService(
                actionContext,
                new TestTimeProvider(now)
            );

            await serviceUnderTest.UpdatePostmortemAsync(
                incidentId,
                "   ",
                null,
                CancellationToken.None
            );
        }

        await using var verificationContext = new ApplicationDbContext(options);
        var incident = await verificationContext
            .Incidents.Include(item => item.Events)
            .SingleAsync(item => item.Id == incidentId);

        Assert.Null(incident.Postmortem);

        var resolvedEvent = Assert.Single(
            incident.Events,
            item => item.EventType == IncidentEventType.Resolved
        );
        Assert.Null(resolvedEvent.Body);
        Assert.DoesNotContain(
            incident.Events,
            item => item.EventType == IncidentEventType.PostmortemPublished
        );
    }

    private static DbContextOptions<ApplicationDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static Service CreateService(string name, string slug) =>
        new()
        {
            ServiceGroup = new ServiceGroup { Name = "Core", Slug = "core" },
            Name = name,
            Slug = slug,
            IsEnabled = true,
            CheckPeriodSeconds = 60,
            FailureThreshold = 3,
            RecoveryThreshold = 3,
            MonitorDefinition = new MonitorDefinition
            {
                MonitorType = MonitorType.Http,
                Url = "https://status.example.com/health",
                HttpMethod = "GET",
                ExpectedStatusCodes = "200-299",
                TimeoutSeconds = 10,
            },
        };
}
