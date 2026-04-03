using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Web.Metadata;
using StatusPageSharp.Web.Models;

namespace StatusPageSharp.Web.Pages.Services;

public class DetailsModel(
    IPublicStatusService publicStatusService,
    IIncidentManagementService incidentManagementService
) : PageModel
{
    public PublicServiceDetailsModel Service { get; private set; } = null!;

    public IncidentHistorySectionModel IncidentHistory { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(string slug, int? page)
    {
        var service = await publicStatusService.GetServiceDetailsAsync(
            slug,
            HttpContext.RequestAborted
        );
        if (service is null)
        {
            return NotFound();
        }

        Service = service;
        var history = await incidentManagementService.GetIncidentHistoryPageAsync(
            service.Id,
            page ?? 1,
            IncidentHistoryDefaults.PageSize,
            HttpContext.RequestAborted
        );
        IncidentHistory = new IncidentHistorySectionModel(
            "Incident History",
            $"No resolved incidents have been published for {service.Name}.",
            "/Services/Details",
            history,
            service.Slug
        );
        ViewData["Title"] = service.Name;
        ViewData["Description"] = SocialMetadataBuilder.BuildServiceDescription(service);
        return Page();
    }
}
