using Microsoft.EntityFrameworkCore;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Domain.Entities;
using StatusPageSharp.Domain.Enums;
using StatusPageSharp.Infrastructure.Data;
using StatusPageSharp.Infrastructure.Services;
using StatusPageSharp.Infrastructure.Tests.Support;

namespace StatusPageSharp.Infrastructure.Tests;

public class AdminCatalogServiceTests
{
    [Fact]
    public async Task CreateServiceAsync_SetsPortToNull_WhenMonitorTypeIsIcmp()
    {
        var options = CreateOptions();
        var serviceGroupId = Guid.NewGuid();

        await using (var seedContext = new ApplicationDbContext(options))
        {
            seedContext.ServiceGroups.Add(
                new ServiceGroup
                {
                    Id = serviceGroupId,
                    Name = "Core",
                    Slug = "core",
                }
            );

            await seedContext.SaveChangesAsync();
        }

        await using (var dbContext = new ApplicationDbContext(options))
        {
            var service = CreateAdminCatalogService(dbContext);
            var model = CreateUpsertModel(serviceGroupId);
            model.MonitorType = MonitorType.Icmp;
            model.Host = "status.example.com";
            model.Port = 443;

            await service.CreateServiceAsync(model, CancellationToken.None);
        }

        await using var verificationContext = new ApplicationDbContext(options);
        var createdService = await verificationContext
            .Services.Include(item => item.MonitorDefinition)
            .SingleAsync();

        Assert.Null(createdService.MonitorDefinition.Port);
    }

    [Fact]
    public async Task UpdateServiceAsync_PreservesPort_WhenIcmpMonitorIsEditedWithoutPort()
    {
        var options = CreateOptions();
        var serviceId = Guid.NewGuid();
        var serviceGroupId = Guid.NewGuid();

        await using (var seedContext = new ApplicationDbContext(options))
        {
            seedContext.ServiceGroups.Add(
                new ServiceGroup
                {
                    Id = serviceGroupId,
                    Name = "Core",
                    Slug = "core",
                }
            );

            seedContext.Services.Add(
                new Service
                {
                    Id = serviceId,
                    ServiceGroupId = serviceGroupId,
                    Name = "API",
                    Slug = "api",
                    MonitorDefinition = new MonitorDefinition
                    {
                        MonitorType = MonitorType.Icmp,
                        Host = "status.example.com",
                        Port = 443,
                        HttpMethod = "GET",
                        ExpectedStatusCodes = "200-299",
                        TimeoutSeconds = 10,
                    },
                }
            );

            await seedContext.SaveChangesAsync();
        }

        await using (var dbContext = new ApplicationDbContext(options))
        {
            var service = CreateAdminCatalogService(dbContext);
            var model = CreateUpsertModel(serviceGroupId);
            model.Name = "API Updated";
            model.MonitorType = MonitorType.Icmp;
            model.Host = "icmp.example.com";
            model.Port = null;

            await service.UpdateServiceAsync(serviceId, model, CancellationToken.None);
        }

        await using var verificationContext = new ApplicationDbContext(options);
        var updatedService = await verificationContext
            .Services.Include(item => item.MonitorDefinition)
            .SingleAsync(item => item.Id == serviceId);

        Assert.Equal("API Updated", updatedService.Name);
        Assert.Equal("icmp.example.com", updatedService.MonitorDefinition.Host);
        Assert.Equal(443, updatedService.MonitorDefinition.Port);
    }

    private static DbContextOptions<ApplicationDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static ServiceUpsertModel CreateUpsertModel(Guid serviceGroupId) =>
        new()
        {
            ServiceGroupId = serviceGroupId,
            Name = "API",
            Slug = "api",
            IsEnabled = true,
            CheckPeriodSeconds = 60,
            FailureThreshold = 3,
            RecoveryThreshold = 3,
            MonitorType = MonitorType.Https,
            Url = "https://status.example.com/health",
            HttpMethod = "GET",
            ExpectedStatusCodes = "200-299",
            TimeoutSeconds = 10,
        };

    private static AdminCatalogService CreateAdminCatalogService(ApplicationDbContext dbContext) =>
        new(dbContext, new TestPublicStatusService(), new TestTimeProvider(DateTime.UtcNow));
}
