using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;

namespace StatusPageSharp.Web.Pages.Admin.Services;

public class IndexModel(IAdminCatalogService adminCatalogService) : AdminPageModel
{
    public IReadOnlyList<ServiceAdminModel> Services { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Services = await adminCatalogService.GetServicesAsync(HttpContext.RequestAborted);
        ViewData["Title"] = "Services";
    }
}
