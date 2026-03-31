using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Web.Metadata;

namespace StatusPageSharp.Web.Pages.Incidents;

public class DetailsModel(IIncidentManagementService incidentManagementService) : PageModel
{
    public PublicIncidentDetailsModel Incident { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var incident = await incidentManagementService.GetIncidentAsync(
            id,
            HttpContext.RequestAborted
        );
        if (incident is null)
        {
            return NotFound();
        }

        Incident = incident;
        ViewData["Title"] = incident.Title;
        ViewData["Description"] = SocialMetadataBuilder.BuildIncidentDescription(incident);
        return Page();
    }
}
