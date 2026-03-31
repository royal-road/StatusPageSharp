using Microsoft.AspNetCore.Mvc;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;

namespace StatusPageSharp.Web.Pages.Admin.Maintenance;

public class IndexModel(
    IMaintenanceManagementService maintenanceManagementService,
    IAdminCatalogService adminCatalogService
) : AdminPageModel
{
    [BindProperty]
    public MaintenanceUpsertModel Input { get; set; } =
        new() { StartsUtc = DateTime.UtcNow, EndsUtc = DateTime.UtcNow.AddHours(1) };

    public IReadOnlyList<ServiceAdminModel> Services { get; private set; } = [];

    public IReadOnlyList<MaintenanceAdminModel> MaintenanceItems { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Services = await adminCatalogService.GetServicesAsync(HttpContext.RequestAborted);
        MaintenanceItems = await maintenanceManagementService.GetAdminMaintenanceAsync(
            HttpContext.RequestAborted
        );
        ViewData["Title"] = "Maintenance";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Services = await adminCatalogService.GetServicesAsync(HttpContext.RequestAborted);
        MaintenanceItems = await maintenanceManagementService.GetAdminMaintenanceAsync(
            HttpContext.RequestAborted
        );
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await maintenanceManagementService.CreateMaintenanceAsync(
            Input,
            CurrentUserId,
            HttpContext.RequestAborted
        );
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await maintenanceManagementService.DeleteMaintenanceAsync(id, HttpContext.RequestAborted);
        return RedirectToPage();
    }
}
