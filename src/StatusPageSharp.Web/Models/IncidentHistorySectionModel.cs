using System.Globalization;
using StatusPageSharp.Application.Models.Public;

namespace StatusPageSharp.Web.Models;

public class IncidentHistorySectionModel
{
    public IncidentHistorySectionModel(
        string title,
        string emptyMessage,
        string pageName,
        PublicIncidentHistoryPageModel history,
        string? serviceSlug = null,
        string anchorId = IncidentHistoryDefaults.AnchorId
    )
    {
        Title = title;
        EmptyMessage = emptyMessage;
        PageName = pageName;
        History = history;
        ServiceSlug = serviceSlug;
        AnchorId = anchorId;
    }

    public string Title { get; }

    public string EmptyMessage { get; }

    public string PageName { get; }

    public PublicIncidentHistoryPageModel History { get; }

    public string? ServiceSlug { get; }

    public string AnchorId { get; }

    public bool HasPreviousPage => History.PageNumber > 1;

    public bool HasNextPage => History.PageNumber < History.TotalPages;

    public Dictionary<string, string> PreviousPageRouteValues =>
        BuildPageRouteValues(History.PageNumber - 1);

    public Dictionary<string, string> NextPageRouteValues =>
        BuildPageRouteValues(History.PageNumber + 1);

    public Dictionary<string, string> BuildPageRouteValues(int pageNumber)
    {
        var normalizedPageNumber = Math.Min(
            Math.Max(1, pageNumber),
            Math.Max(History.TotalPages, 1)
        );
        var routeValues = new Dictionary<string, string>
        {
            ["page"] = normalizedPageNumber.ToString(CultureInfo.InvariantCulture),
        };

        if (!string.IsNullOrWhiteSpace(ServiceSlug))
        {
            routeValues["slug"] = ServiceSlug;
        }

        return routeValues;
    }
}
