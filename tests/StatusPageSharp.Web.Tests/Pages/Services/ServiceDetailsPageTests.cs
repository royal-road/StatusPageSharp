using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Domain.Enums;
using ServiceDetailsModel = StatusPageSharp.Web.Pages.Services.DetailsModel;

namespace StatusPageSharp.Web.Tests.Pages.Services;

public class ServiceDetailsPageTests
{
    [Fact]
    public async Task OnGetAsync_ReturnsPageAndLoadsFilteredIncidentHistory()
    {
        var service = CreateServiceDetails();
        var publicStatusService = new TestPublicStatusService { Service = service };
        var incidentService = new TestIncidentManagementService
        {
            HistoryPage = new PublicIncidentHistoryPageModel(
                [
                    new PublicIncidentSummaryModel(
                        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        "Resolved API incident",
                        "Recovered.",
                        IncidentStatus.Resolved,
                        DateTime.Parse("2026-04-01T10:00:00Z").ToUniversalTime(),
                        DateTime.Parse("2026-04-01T11:00:00Z").ToUniversalTime(),
                        ["API"]
                    ),
                ],
                2,
                10,
                12,
                2
            ),
        };
        var model = CreateModel(publicStatusService, incidentService);

        var result = await model.OnGetAsync(service.Slug, 2);

        Assert.IsType<PageResult>(result);
        Assert.Equal(service.Id, incidentService.LastServiceId);
        Assert.Equal(2, incidentService.LastPageNumber);
        Assert.Equal(10, incidentService.LastPageSize);
        Assert.Equal("/Services/Details", model.IncidentHistory.PageName);
        Assert.Equal(service.Slug, model.IncidentHistory.ServiceSlug);
        Assert.Equal(service.Name, model.Service.Name);
    }

    [Fact]
    public async Task OnGetAsync_UsesEmptyStateMessage_WhenServiceHistoryIsEmpty()
    {
        var service = CreateServiceDetails();
        var publicStatusService = new TestPublicStatusService { Service = service };
        var incidentService = new TestIncidentManagementService
        {
            HistoryPage = new PublicIncidentHistoryPageModel([], 1, 10, 0, 1),
        };
        var model = CreateModel(publicStatusService, incidentService);

        var result = await model.OnGetAsync(service.Slug, null);

        Assert.IsType<PageResult>(result);
        Assert.Equal(
            $"No resolved incidents have been published for {service.Name}.",
            model.IncidentHistory.EmptyMessage
        );
        Assert.Empty(model.IncidentHistory.History.Items);
    }

    [Fact]
    public async Task OnGetAsync_ReturnsNotFound_WhenServiceDoesNotExist()
    {
        var publicStatusService = new TestPublicStatusService();
        var incidentService = new TestIncidentManagementService();
        var model = CreateModel(publicStatusService, incidentService);

        var result = await model.OnGetAsync("missing", null);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(0, incidentService.GetIncidentHistoryPageCallCount);
    }

    private static ServiceDetailsModel CreateModel(
        IPublicStatusService publicStatusService,
        IIncidentManagementService incidentManagementService
    )
    {
        var model = new ServiceDetailsModel(publicStatusService, incidentManagementService)
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

    private static PublicServiceDetailsModel CreateServiceDetails() =>
        new(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "Core",
            "API",
            "api",
            "Primary API",
            ServiceStatus.Operational,
            120,
            99.95m,
            [],
            [],
            [],
            []
        );

    private class TestPublicStatusService : IPublicStatusService
    {
        public PublicServiceDetailsModel? Service { get; set; }

        public Task<PublicSiteSummaryModel> GetSiteSummaryAsync(
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<PublicServiceDetailsModel?> GetServiceDetailsAsync(
            string slug,
            CancellationToken cancellationToken
        ) => Task.FromResult(Service?.Slug == slug ? Service : null);

        public Task<ServiceHistorySeriesModel?> GetServiceHistoryAsync(
            string slug,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
    }

    private class TestIncidentManagementService : IIncidentManagementService
    {
        public PublicIncidentHistoryPageModel HistoryPage { get; set; } = new([], 1, 10, 0, 1);

        public int GetIncidentHistoryPageCallCount { get; private set; }

        public Guid? LastServiceId { get; private set; }

        public int LastPageNumber { get; private set; }

        public int LastPageSize { get; private set; }

        public Task<IReadOnlyList<PublicIncidentSummaryModel>> GetIncidentSummariesAsync(
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<PublicIncidentHistoryPageModel> GetIncidentHistoryPageAsync(
            Guid? serviceId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken
        )
        {
            GetIncidentHistoryPageCallCount++;
            LastServiceId = serviceId;
            LastPageNumber = pageNumber;
            LastPageSize = pageSize;
            return Task.FromResult(HistoryPage);
        }

        public Task<PublicIncidentDetailsModel?> GetIncidentAsync(
            Guid id,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<IReadOnlyList<IncidentAdminModel>> GetAdminIncidentsAsync(
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<IncidentAdminModel?> GetAdminIncidentAsync(
            Guid id,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<Guid> CreateManualIncidentAsync(
            ManualIncidentUpsertModel model,
            string? userId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task<Guid> CreateHistoricalIncidentAsync(
            HistoricalIncidentCreateModel model,
            string? userId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task AddManualEventAsync(
            Guid id,
            IncidentEventUpsertModel model,
            string? userId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task UpdateAffectedServicesAsync(
            Guid id,
            IncidentAffectedServicesUpdateModel model,
            string? userId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task ResolveIncidentAsync(
            Guid id,
            string? postmortem,
            string? userId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task MergeIncidentAsync(
            Guid targetId,
            Guid sourceId,
            string? userId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();

        public Task DeleteIncidentAsync(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task UpdatePostmortemAsync(
            Guid id,
            string? postmortem,
            string? userId,
            CancellationToken cancellationToken
        ) => throw new NotSupportedException();
    }
}
