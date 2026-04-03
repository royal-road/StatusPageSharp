using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Domain.Enums;
using IncidentIndexModel = StatusPageSharp.Web.Pages.Incidents.IndexModel;

namespace StatusPageSharp.Web.Tests.Pages.Incidents;

public class IncidentIndexPageTests
{
    [Fact]
    public async Task OnGetAsync_LoadsRequestedIncidentHistoryPage()
    {
        var incidentService = new TestIncidentManagementService
        {
            HistoryPage = new PublicIncidentHistoryPageModel(
                [
                    new PublicIncidentSummaryModel(
                        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                        "Resolved incident",
                        "Recovered after rollback.",
                        IncidentStatus.Resolved,
                        DateTime.Parse("2026-04-01T10:00:00Z").ToUniversalTime(),
                        DateTime.Parse("2026-04-01T11:00:00Z").ToUniversalTime(),
                        ["API"]
                    ),
                ],
                2,
                10,
                11,
                2
            ),
        };
        var model = CreateModel(incidentService);

        await model.OnGetAsync(2);

        Assert.Equal(2, incidentService.LastPageNumber);
        Assert.Equal(10, incidentService.LastPageSize);
        Assert.Null(incidentService.LastServiceId);
        Assert.Equal("Incidents", model.ViewData["Title"]);
        Assert.Equal(2, model.IncidentHistory.History.PageNumber);
        Assert.Equal("/Incidents/Index", model.IncidentHistory.PageName);
        Assert.Equal("Resolved Incidents", model.IncidentHistory.Title);
    }

    [Fact]
    public async Task OnGetAsync_UsesEmptyStateMessage_WhenHistoryIsEmpty()
    {
        var incidentService = new TestIncidentManagementService
        {
            HistoryPage = new PublicIncidentHistoryPageModel([], 1, 10, 0, 1),
        };
        var model = CreateModel(incidentService);

        await model.OnGetAsync(null);

        Assert.Equal(
            "No resolved incidents have been published yet.",
            model.IncidentHistory.EmptyMessage
        );
        Assert.Empty(model.IncidentHistory.History.Items);
    }

    private static IncidentIndexModel CreateModel(
        IIncidentManagementService incidentManagementService
    )
    {
        var model = new IncidentIndexModel(incidentManagementService)
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

    private class TestIncidentManagementService : IIncidentManagementService
    {
        public PublicIncidentHistoryPageModel HistoryPage { get; set; } = new([], 1, 10, 0, 1);

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
