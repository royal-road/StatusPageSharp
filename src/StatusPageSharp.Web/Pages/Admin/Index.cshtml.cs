using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;

namespace StatusPageSharp.Web.Pages.Admin;

public class IndexModel(IAdminCatalogService adminCatalogService) : AdminPageModel
{
    public AdminDashboardModel Dashboard { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        Dashboard = await adminCatalogService.GetDashboardAsync(HttpContext.RequestAborted);
        ViewData["Title"] = "Admin";
    }
}
