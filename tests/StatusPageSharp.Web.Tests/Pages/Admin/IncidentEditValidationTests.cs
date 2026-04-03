using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Domain.Enums;
using StatusPageSharp.Web.Pages.Admin.Incidents;

namespace StatusPageSharp.Web.Tests.Pages.Admin;

public class IncidentEditValidationTests
{
    [Fact]
    public async Task OnPostAddEventAsync_ReturnsPage_WhenModelStateIsInvalid()
    {
        var incidentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var incidentService = new TestIncidentManagementService
        {
            AdminIncident = CreateIncident(incidentId),
        };
        var adminCatalogService = new TestAdminCatalogService { Services = [CreateService()] };
        var model = CreateModel(incidentService, adminCatalogService);
        model.ModelState.AddModelError(nameof(EditModel.NewEvent), "Message is required.");

        var result = await model.OnPostAddEventAsync(incidentId);

        Assert.IsType<PageResult>(result);
        Assert.Equal(0, incidentService.AddManualEventCallCount);
        Assert.Single(model.AvailableServices);
        Assert.Equal("Database API", model.ViewData["Title"]);
    }

    [Fact]
    public async Task OnPostAddServiceAsync_ReturnsPage_WhenServiceIdIsEmpty()
    {
        var incidentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var incidentService = new TestIncidentManagementService
        {
            AdminIncident = CreateIncident(incidentId),
        };
        var adminCatalogService = new TestAdminCatalogService { Services = [CreateService()] };
        var model = CreateModel(incidentService, adminCatalogService);

        var result = await model.OnPostAddServiceAsync(incidentId);

        Assert.IsType<PageResult>(result);
        Assert.Equal(0, incidentService.UpdateAffectedServicesCallCount);
        Assert.True(model.ModelState.ContainsKey(nameof(EditModel.NewServiceId)));
    }

    [Fact]
    public async Task OnPostDeleteAsync_RedirectsToIncidentIndex_WhenIncidentExists()
    {
        var incidentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var incidentService = new TestIncidentManagementService
        {
            AdminIncident = CreateIncident(incidentId),
        };
        var adminCatalogService = new TestAdminCatalogService { Services = [CreateService()] };
        var model = CreateModel(incidentService, adminCatalogService);

        var result = await model.OnPostDeleteAsync(incidentId);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Admin/Incidents/Index", redirect.PageName);
        Assert.Equal(1, incidentService.DeleteIncidentCallCount);
    }

    [Fact]
    public async Task OnPostMergeAsync_ReturnsPage_WhenIncidentIsNotSelected()
    {
        var incidentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var incidentService = new TestIncidentManagementService
        {
            AdminIncident = CreateIncident(incidentId),
        };
        var adminCatalogService = new TestAdminCatalogService { Services = [CreateService()] };
        var model = CreateModel(incidentService, adminCatalogService);

        var result = await model.OnPostMergeAsync(incidentId);

        Assert.IsType<PageResult>(result);
        Assert.Equal(0, incidentService.MergeIncidentCallCount);
        Assert.True(model.ModelState.ContainsKey(nameof(EditModel.MergeSourceIncidentId)));
    }

    [Fact]
    public async Task OnPostMergeAsync_RedirectsToPage_WhenIncidentIsSelected()
    {
        var incidentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var sourceIncidentId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var sourceIncident = CreateIncident(sourceIncidentId) with
        {
            Title = "Database API follow-up",
            StartedUtc = DateTime.Parse("2026-04-03T12:10:00Z").ToUniversalTime(),
        };
        var incidentService = new TestIncidentManagementService
        {
            AdminIncident = CreateIncident(incidentId),
            Incidents = [CreateIncident(incidentId), sourceIncident],
        };
        var adminCatalogService = new TestAdminCatalogService { Services = [CreateService()] };
        var model = CreateModel(incidentService, adminCatalogService);
        model.MergeSourceIncidentId = sourceIncidentId;

        var result = await model.OnPostMergeAsync(incidentId);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Null(redirect.PageName);
        Assert.Equal(1, incidentService.MergeIncidentCallCount);
    }

    private static EditModel CreateModel(
        IIncidentManagementService incidentManagementService,
        IAdminCatalogService adminCatalogService
    )
    {
        var model = new EditModel(incidentManagementService, adminCatalogService)
        {
            PageContext = new PageContext
            {
                HttpContext = new DefaultHttpContext(),
                ViewData = new ViewDataDictionary(
                    new EmptyModelMetadataProvider(),
                    new ModelStateDictionary()
                ),
            },
        };

        return model;
    }

    private static IncidentAdminModel CreateIncident(Guid incidentId) =>
        new(
            incidentId,
            "Database API",
            "Primary API is degraded.",
            IncidentSource.Manual,
            IncidentStatus.Open,
            DateTime.Parse("2026-04-03T12:00:00Z").ToUniversalTime(),
            null,
            null,
            [],
            []
        );

    private static ServiceAdminModel CreateService() =>
        new(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "Core",
            "API",
            "api",
            null,
            true,
            0,
            60,
            3,
            3,
            null,
            MonitorType.Https,
            null,
            null,
            "https://example.com/health",
            "GET",
            null,
            null,
            "200-299",
            null,
            true,
            10
        );

    private class TestIncidentManagementService : IIncidentManagementService
    {
        public IncidentAdminModel? AdminIncident { get; set; }

        public IReadOnlyList<IncidentAdminModel> Incidents { get; set; } = [];

        public int AddManualEventCallCount { get; private set; }

        public int UpdateAffectedServicesCallCount { get; private set; }

        public int MergeIncidentCallCount { get; private set; }

        public int DeleteIncidentCallCount { get; private set; }

        public Task AddManualEventAsync(
            Guid id,
            IncidentEventUpsertModel model,
            string? userId,
            CancellationToken cancellationToken
        )
        {
            AddManualEventCallCount++;
            return Task.CompletedTask;
        }

        public Task<Guid> CreateHistoricalIncidentAsync(
            HistoricalIncidentCreateModel model,
            string? userId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<Guid> CreateManualIncidentAsync(
            ManualIncidentUpsertModel model,
            string? userId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task MergeIncidentAsync(
            Guid targetId,
            Guid sourceId,
            string? userId,
            CancellationToken cancellationToken
        )
        {
            MergeIncidentCallCount++;
            return Task.CompletedTask;
        }

        public Task DeleteIncidentAsync(Guid id, CancellationToken cancellationToken)
        {
            DeleteIncidentCallCount++;
            return Task.CompletedTask;
        }

        public Task<IncidentAdminModel?> GetAdminIncidentAsync(
            Guid id,
            CancellationToken cancellationToken
        ) => Task.FromResult(AdminIncident);

        public Task<IReadOnlyList<IncidentAdminModel>> GetAdminIncidentsAsync(
            CancellationToken cancellationToken
        )
        {
            if (Incidents.Count > 0)
            {
                return Task.FromResult(Incidents);
            }

            return Task.FromResult<IReadOnlyList<IncidentAdminModel>>(
                AdminIncident is null ? [] : [AdminIncident]
            );
        }

        public Task<PublicIncidentDetailsModel?> GetIncidentAsync(
            Guid id,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<IReadOnlyList<PublicIncidentSummaryModel>> GetIncidentSummariesAsync(
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<PublicIncidentHistoryPageModel> GetIncidentHistoryPageAsync(
            Guid? serviceId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task ResolveIncidentAsync(
            Guid id,
            string? postmortem,
            string? userId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task UpdateAffectedServicesAsync(
            Guid id,
            IncidentAffectedServicesUpdateModel model,
            string? userId,
            CancellationToken cancellationToken
        )
        {
            UpdateAffectedServicesCallCount++;
            return Task.CompletedTask;
        }

        public Task UpdatePostmortemAsync(
            Guid id,
            string? postmortem,
            string? userId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
    }

    private class TestAdminCatalogService : IAdminCatalogService
    {
        public IReadOnlyList<ServiceAdminModel> Services { get; set; } = [];

        public Task CreateServiceAsync(
            ServiceUpsertModel model,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task CreateServiceGroupAsync(
            ServiceGroupUpsertModel model,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<AdminDashboardModel> GetDashboardAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<ServiceAdminModel?> GetServiceAsync(
            Guid id,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<IReadOnlyList<ServiceGroupAdminModel>> GetServiceGroupsAsync(
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<IReadOnlyList<ServiceAdminModel>> GetServicesAsync(
            CancellationToken cancellationToken
        ) => Task.FromResult(Services);

        public Task UpdateServiceAsync(
            Guid id,
            ServiceUpsertModel model,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task UpdateServiceGroupAsync(
            Guid id,
            ServiceGroupUpsertModel model,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
    }
}
