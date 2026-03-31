using Microsoft.AspNetCore.Mvc;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Web.Pages.Admin.Incidents;

public class EditModel(
    IIncidentManagementService incidentManagementService,
    IAdminCatalogService adminCatalogService
) : AdminPageModel
{
    public IncidentAdminModel Incident { get; private set; } = null!;

    public IReadOnlyList<ServiceAdminModel> AvailableServices { get; private set; } = [];

    [BindProperty]
    public IncidentEventUpsertModel NewEvent { get; set; } = new();

    [BindProperty]
    public Guid NewServiceId { get; set; }

    [BindProperty]
    public IncidentImpactLevel NewServiceImpactLevel { get; set; } =
        IncidentImpactLevel.MajorOutage;

    [BindProperty]
    public string? Postmortem { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var incident = await incidentManagementService.GetAdminIncidentAsync(
            id,
            HttpContext.RequestAborted
        );
        if (incident is null)
        {
            return NotFound();
        }

        Incident = incident;
        Postmortem = incident.Postmortem;
        AvailableServices = await adminCatalogService.GetServicesAsync(HttpContext.RequestAborted);
        ViewData["Title"] = incident.Title;
        return Page();
    }

    public async Task<IActionResult> OnPostAddEventAsync(Guid id)
    {
        await incidentManagementService.AddManualEventAsync(
            id,
            NewEvent,
            CurrentUserId,
            HttpContext.RequestAborted
        );
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostAddServiceAsync(Guid id)
    {
        var incident = await incidentManagementService.GetAdminIncidentAsync(
            id,
            HttpContext.RequestAborted
        );
        if (incident is null)
        {
            return NotFound();
        }

        var services = incident
            .AffectedServices.Where(item => !item.IsResolved)
            .Select(item => new IncidentAffectedServiceInputModel
            {
                ServiceId = item.ServiceId,
                ImpactLevel = item.ImpactLevel,
            })
            .ToList();

        if (services.All(item => item.ServiceId != NewServiceId))
        {
            services.Add(
                new IncidentAffectedServiceInputModel
                {
                    ServiceId = NewServiceId,
                    ImpactLevel = NewServiceImpactLevel,
                }
            );
        }

        await incidentManagementService.UpdateAffectedServicesAsync(
            id,
            new IncidentAffectedServicesUpdateModel { Services = services },
            CurrentUserId,
            HttpContext.RequestAborted
        );
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostResolveAsync(Guid id)
    {
        await incidentManagementService.ResolveIncidentAsync(
            id,
            Postmortem,
            CurrentUserId,
            HttpContext.RequestAborted
        );
        return RedirectToPage(new { id });
    }
}
