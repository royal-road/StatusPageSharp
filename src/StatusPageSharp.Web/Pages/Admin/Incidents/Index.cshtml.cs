using Microsoft.AspNetCore.Mvc;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Web.Pages.Admin.Incidents;

public class IndexModel(
    IIncidentManagementService incidentManagementService,
    IAdminCatalogService adminCatalogService
) : AdminPageModel
{
    [BindProperty]
    public ManualIncidentUpsertModel Input { get; set; } = new();

    [BindProperty]
    public Guid SelectedServiceId { get; set; }

    [BindProperty]
    public IncidentImpactLevel SelectedImpactLevel { get; set; } = IncidentImpactLevel.MajorOutage;

    public IReadOnlyList<IncidentAdminModel> Incidents { get; private set; } = [];

    public IReadOnlyList<ServiceAdminModel> Services { get; private set; } = [];

    public IncidentAdminModel? ActiveIncident { get; private set; }

    public async Task OnGetAsync()
    {
        Incidents = await incidentManagementService.GetAdminIncidentsAsync(
            HttpContext.RequestAborted
        );
        Services = await adminCatalogService.GetServicesAsync(HttpContext.RequestAborted);
        ActiveIncident = Incidents.FirstOrDefault(incident =>
            incident.Status == IncidentStatus.Open
        );
        ViewData["Title"] = "Incidents";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Input.Services =
            SelectedServiceId == Guid.Empty
                ? []
                :
                [
                    new IncidentAffectedServiceInputModel
                    {
                        ServiceId = SelectedServiceId,
                        ImpactLevel = SelectedImpactLevel,
                    },
                ];

        if (SelectedServiceId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(SelectedServiceId), "An affected service is required.");
        }

        Incidents = await incidentManagementService.GetAdminIncidentsAsync(
            HttpContext.RequestAborted
        );
        Services = await adminCatalogService.GetServicesAsync(HttpContext.RequestAborted);
        ActiveIncident = Incidents.FirstOrDefault(incident =>
            incident.Status == IncidentStatus.Open
        );
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var incidentId = await incidentManagementService.CreateManualIncidentAsync(
            Input,
            CurrentUserId,
            HttpContext.RequestAborted
        );
        return RedirectToPage("/Admin/Incidents/Edit", new { id = incidentId });
    }
}
