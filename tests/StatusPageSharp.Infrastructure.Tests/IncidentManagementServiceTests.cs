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
