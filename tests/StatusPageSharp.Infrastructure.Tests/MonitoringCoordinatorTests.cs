using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using StatusPageSharp.Domain.Entities;
using StatusPageSharp.Domain.Enums;
using StatusPageSharp.Infrastructure.Data;
using StatusPageSharp.Infrastructure.Monitoring;
using StatusPageSharp.Infrastructure.Services;
using StatusPageSharp.Infrastructure.Tests.Support;

namespace StatusPageSharp.Infrastructure.Tests;

public class MonitoringCoordinatorTests
{
    [Fact]
    public async Task ExecuteCheckAsync_ResolvesExistingIncidentWithoutConcurrencyFailure()
    {
        var now = new DateTime(2026, 3, 30, 12, 0, 0, DateTimeKind.Utc);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Guid serviceId;
        await using (var seedContext = new ApplicationDbContext(options))
        {
            var serviceGroup = new ServiceGroup { Name = "Core", Slug = "core" };

            var service = new Service
            {
                ServiceGroup = serviceGroup,
                Name = "API",
                Slug = "api",
                IsEnabled = true,
                CheckPeriodSeconds = 60,
                FailureThreshold = 3,
                RecoveryThreshold = 3,
                ConsecutiveSuccessCount = 2,
                NextCheckUtc = now.AddMinutes(-1),
                MonitorDefinition = new MonitorDefinition
                {
                    MonitorType = MonitorType.Http,
                    Url = "https://status.example.com/health",
                    HttpMethod = "GET",
                    ExpectedStatusCodes = "200-299",
                    TimeoutSeconds = 10,
                },
            };

            seedContext.Incidents.Add(
                new Incident
                {
                    Source = IncidentSource.Automatic,
                    Status = IncidentStatus.Open,
                    Title = "API degraded",
                    Summary = "Recovering",
                    StartedUtc = now.AddMinutes(-10),
                    AffectedServices =
                    [
                        new IncidentAffectedService
                        {
                            Service = service,
                            ImpactLevel = IncidentImpactLevel.MajorOutage,
                            AddedUtc = now.AddMinutes(-10),
                        },
                    ],
                    Events =
                    [
                        new IncidentEvent
                        {
                            EventType = IncidentEventType.Created,
                            Message = "Incident opened",
                            CreatedUtc = now.AddMinutes(-10),
                            IsSystemGenerated = true,
                        },
                    ],
                }
            );

            await seedContext.SaveChangesAsync();
            serviceId = service.Id;
        }

        var httpClient = new HttpClient(
            new TestHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("ok"),
            })
        );
        var coordinator = new MonitoringCoordinator(
            new TestDbContextFactory(options),
            new MonitorProbeClient(new TestHttpClientFactory(httpClient)),
            new TestClock(now)
        );

        var result = await coordinator.ExecuteCheckAsync(serviceId, CancellationToken.None);

        Assert.True(result.IsSuccess);

        await using var verificationContext = new ApplicationDbContext(options);
        var incident = await verificationContext
            .Incidents.Include(item => item.AffectedServices)
            .Include(item => item.Events)
            .SingleAsync();

        Assert.Equal(IncidentStatus.Resolved, incident.Status);
        Assert.Equal(3, incident.Events.Count);
        Assert.All(incident.AffectedServices, item => Assert.True(item.IsResolved));
    }
}
