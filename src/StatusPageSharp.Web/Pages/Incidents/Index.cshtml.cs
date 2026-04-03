using Microsoft.AspNetCore.Mvc.RazorPages;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Web.Metadata;
using StatusPageSharp.Web.Models;

namespace StatusPageSharp.Web.Pages.Incidents;

public class IndexModel(IIncidentManagementService incidentManagementService) : PageModel
{
    public IncidentHistorySectionModel IncidentHistory { get; private set; } = null!;

    public async Task OnGetAsync(int? page)
    {
        var history = await incidentManagementService.GetIncidentHistoryPageAsync(
            null,
            page ?? 1,
            IncidentHistoryDefaults.PageSize,
            HttpContext.RequestAborted
        );

        IncidentHistory = new IncidentHistorySectionModel(
            "Resolved Incidents",
            "No resolved incidents have been published yet.",
            "/Incidents/Index",
            history
        );

        ViewData["Title"] = "Incidents";
        ViewData["Description"] = SocialMetadataBuilder.BuildIncidentHistoryDescription(history);
    }
}
