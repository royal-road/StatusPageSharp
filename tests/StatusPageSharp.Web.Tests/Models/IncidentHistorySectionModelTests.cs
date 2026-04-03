using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Domain.Enums;
using StatusPageSharp.Web.Models;

namespace StatusPageSharp.Web.Tests.Models;

public class IncidentHistorySectionModelTests
{
    [Fact]
    public void PreviousPageRouteValues_UsesOnlyPageForGlobalHistory()
    {
        var model = new IncidentHistorySectionModel(
            "Resolved Incidents",
            "No incidents.",
            "/Incidents/Index",
            new PublicIncidentHistoryPageModel([CreateIncidentSummary()], 2, 10, 20, 3)
        );

        Assert.Equal("1", model.PreviousPageRouteValues["page"]);
        Assert.False(model.PreviousPageRouteValues.ContainsKey("slug"));
        Assert.Equal("3", model.NextPageRouteValues["page"]);
    }

    [Fact]
    public void BuildPageRouteValues_IncludesSlugForServiceHistory()
    {
        var model = new IncidentHistorySectionModel(
            "Incident History",
            "No incidents.",
            "/Services/Details",
            new PublicIncidentHistoryPageModel([CreateIncidentSummary()], 1, 10, 10, 2),
            "api"
        );

        var routeValues = model.BuildPageRouteValues(5);

        Assert.Equal("api", routeValues["slug"]);
        Assert.Equal("2", routeValues["page"]);
    }

    private static PublicIncidentSummaryModel CreateIncidentSummary() =>
        new(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "Resolved incident",
            "Recovered.",
            IncidentStatus.Resolved,
            DateTime.Parse("2026-04-01T10:00:00Z").ToUniversalTime(),
            DateTime.Parse("2026-04-01T11:00:00Z").ToUniversalTime(),
            ["API"]
        );
}
