using Microsoft.AspNetCore.Mvc.RazorPages;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Public;

namespace StatusPageSharp.Web.Pages.Maintenance;

public class IndexModel(IMaintenanceManagementService maintenanceManagementService) : PageModel
{
    public IReadOnlyList<PublicMaintenanceSummaryModel> MaintenanceItems { get; private set; } = [];

    public async Task OnGetAsync()
    {
        MaintenanceItems = await maintenanceManagementService.GetPublicMaintenanceAsync(
            HttpContext.RequestAborted
        );
        ViewData["Title"] = "Maintenance";
    }
}
