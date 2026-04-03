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

    public IReadOnlyList<IncidentAdminModel> MergeCandidates { get; private set; } = [];

    [BindProperty]
    public IncidentEventUpsertModel NewEvent { get; set; } = new();

    [BindProperty]
    public Guid NewServiceId { get; set; }

    [BindProperty]
    public IncidentImpactLevel NewServiceImpactLevel { get; set; } =
        IncidentImpactLevel.MajorOutage;

    [BindProperty]
    public string? Postmortem { get; set; }

    [BindProperty]
    public Guid MergeSourceIncidentId { get; set; }

    public bool CanModifyTimeline => Incident is { Status: IncidentStatus.Open };

    public bool CanUpdateResolvedPostmortem => Incident is { Status: IncidentStatus.Resolved };

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var incident = await LoadIncidentAsync(id);
        if (incident is null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddEventAsync(Guid id)
    {
        var incident = await LoadIncidentAsync(id);
        if (incident is null)
        {
            return NotFound();
        }

        if (incident.Status == IncidentStatus.Resolved)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

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
        var incident = await LoadIncidentAsync(id);
        if (incident is null)
        {
            return NotFound();
        }

        if (incident.Status == IncidentStatus.Resolved)
        {
            return BadRequest();
        }

        if (NewServiceId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(NewServiceId), "A service is required.");
            return Page();
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
        var incident = await incidentManagementService.GetAdminIncidentAsync(
            id,
            HttpContext.RequestAborted
        );
        if (incident is null)
        {
            return NotFound();
        }

        if (incident.Status == IncidentStatus.Resolved)
        {
            return BadRequest();
        }

        await incidentManagementService.ResolveIncidentAsync(
            id,
            Postmortem,
            CurrentUserId,
            HttpContext.RequestAborted
        );
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostMergeAsync(Guid id)
    {
        var incident = await LoadIncidentAsync(id);
        if (incident is null)
        {
            return NotFound();
        }

        if (MergeSourceIncidentId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(MergeSourceIncidentId),
                "Select an incident to merge into this one."
            );
            return Page();
        }

        try
        {
            await incidentManagementService.MergeIncidentAsync(
                id,
                MergeSourceIncidentId,
                CurrentUserId,
                HttpContext.RequestAborted
            );
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(nameof(MergeSourceIncidentId), exception.Message);
            return Page();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        var incident = await incidentManagementService.GetAdminIncidentAsync(
            id,
            HttpContext.RequestAborted
        );
        if (incident is null)
        {
            return NotFound();
        }

        await incidentManagementService.DeleteIncidentAsync(id, HttpContext.RequestAborted);
        return RedirectToPage("/Admin/Incidents/Index");
    }

    public async Task<IActionResult> OnPostSavePostmortemAsync(Guid id)
    {
        var incident = await incidentManagementService.GetAdminIncidentAsync(
            id,
            HttpContext.RequestAborted
        );
        if (incident is null)
        {
            return NotFound();
        }

        if (incident.Status != IncidentStatus.Resolved)
        {
            return BadRequest();
        }

        await incidentManagementService.UpdatePostmortemAsync(
            id,
            Postmortem,
            CurrentUserId,
            HttpContext.RequestAborted
        );
        return RedirectToPage(new { id });
    }

    private async Task<IncidentAdminModel?> LoadIncidentAsync(Guid id)
    {
        var incident = await incidentManagementService.GetAdminIncidentAsync(
            id,
            HttpContext.RequestAborted
        );
        if (incident is null)
        {
            return null;
        }

        Incident = incident;
        Postmortem = incident.Postmortem;
        AvailableServices = await adminCatalogService.GetServicesAsync(HttpContext.RequestAborted);
        MergeCandidates = (
            await incidentManagementService.GetAdminIncidentsAsync(HttpContext.RequestAborted)
        )
            .Where(item => item.Id != id)
            .ToList();
        ViewData["Title"] = incident.Title;
        return incident;
    }
}
