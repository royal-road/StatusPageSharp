using Microsoft.EntityFrameworkCore;
using StatusPageSharp.Domain.Entities;
using StatusPageSharp.Domain.Enums;
using StatusPageSharp.Infrastructure.Data;
using StatusPageSharp.Infrastructure.Services;
using StatusPageSharp.Infrastructure.Tests.Support;

namespace StatusPageSharp.Infrastructure.Tests;

public class PublicStatusServiceTests
{
    [Fact]
    public async Task GetServiceDetailsAsync_UsesDailyRollupIncidentCountForIncidentDays()
    {
        await using var dbContext = CreateDbContext();
        var now = new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc);
        var incidentDay = DateOnly.FromDateTime(now.AddDays(-1));
        var service = new Service
        {
            ServiceGroup = new ServiceGroup { Name = "Core", Slug = "core" },
            Name = "API",
            Slug = "api",
            IsEnabled = true,
            CheckPeriodSeconds = 60,
            MonitorDefinition = new MonitorDefinition
            {
                MonitorType = MonitorType.Http,
                Url = "https://status.example.com/health",
                HttpMethod = "GET",
                ExpectedStatusCodes = "200-299",
                TimeoutSeconds = 10,
            },
        };

        dbContext.Services.Add(service);
        dbContext.DailyServiceRollups.Add(
            new DailyServiceRollup
            {
                Service = service,
                Day = incidentDay,
                AvailabilityEligibleChecks = 10,
                AvailabilitySuccessChecks = 10,
                IncidentCount = 1,
            }
        );

        await dbContext.SaveChangesAsync();

        var serviceUnderTest = new PublicStatusService(dbContext, new TestTimeProvider(now));

        var details = await serviceUnderTest.GetServiceDetailsAsync(
            service.Slug,
            CancellationToken.None
        );

        Assert.NotNull(details);
        var dailyStatus = Assert.Single(details!.DailyStatuses);
        Assert.True(dailyStatus.IsOperational);
        Assert.True(dailyStatus.HasIncidents);
        Assert.Equal(100m, dailyStatus.UptimePercentage);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }
}
