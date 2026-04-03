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
    public ManualIncidentUpsertModel Input { get; set; } = new();

    public HistoricalIncidentCreateModel HistoricalInput { get; set; } = new();

    public Guid SelectedServiceId { get; set; }

    public IncidentImpactLevel SelectedImpactLevel { get; set; } = IncidentImpactLevel.MajorOutage;

    public Guid HistoricalSelectedServiceId { get; set; }

    public IncidentImpactLevel HistoricalSelectedImpactLevel { get; set; } =
        IncidentImpactLevel.MajorOutage;

    public IReadOnlyList<IncidentAdminModel> Incidents { get; private set; } = [];

    public IReadOnlyList<ServiceAdminModel> Services { get; private set; } = [];

    public IncidentAdminModel? ActiveIncident { get; private set; }

    public async Task OnGetAsync()
    {
        await LoadPageStateAsync();
    }

    public async Task<IActionResult> OnPostAsync(
        Guid selectedServiceId,
        IncidentImpactLevel selectedImpactLevel
    )
    {
        await LoadPageStateAsync();

        SelectedServiceId = selectedServiceId;
        SelectedImpactLevel = selectedImpactLevel;

        if (!await TryUpdateModelAsync(Input, nameof(Input)))
        {
            return Page();
        }

        Input.Services = BuildServices(SelectedServiceId, SelectedImpactLevel);

        if (SelectedServiceId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(SelectedServiceId), "An affected service is required.");
        }

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

    public async Task<IActionResult> OnPostBackfillAsync(
        Guid historicalSelectedServiceId,
        IncidentImpactLevel historicalSelectedImpactLevel
    )
    {
        await LoadPageStateAsync();

        HistoricalSelectedServiceId = historicalSelectedServiceId;
        HistoricalSelectedImpactLevel = historicalSelectedImpactLevel;

        if (!await TryUpdateModelAsync(HistoricalInput, nameof(HistoricalInput)))
        {
            return Page();
        }

        HistoricalInput.Services = BuildServices(
            HistoricalSelectedServiceId,
            HistoricalSelectedImpactLevel
        );

        if (HistoricalSelectedServiceId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(HistoricalSelectedServiceId),
                "An affected service is required."
            );
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var incidentId = await incidentManagementService.CreateHistoricalIncidentAsync(
                HistoricalInput,
                CurrentUserId,
                HttpContext.RequestAborted
            );

            return RedirectToPage("/Admin/Incidents/Edit", new { id = incidentId });
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);

            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await incidentManagementService.DeleteIncidentAsync(id, HttpContext.RequestAborted);
        return RedirectToPage();
    }

    private static List<IncidentAffectedServiceInputModel> BuildServices(
        Guid serviceId,
        IncidentImpactLevel impactLevel
    ) =>
        serviceId == Guid.Empty
            ? []
            :
            [
                new IncidentAffectedServiceInputModel
                {
                    ServiceId = serviceId,
                    ImpactLevel = impactLevel,
                },
            ];

    private async Task LoadPageStateAsync()
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
}
