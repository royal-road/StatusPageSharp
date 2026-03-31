using Microsoft.AspNetCore.Mvc;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;

namespace StatusPageSharp.Web.Pages.Admin.ServiceGroups;

public class IndexModel(IAdminCatalogService adminCatalogService) : AdminPageModel
{
    [BindProperty]
    public ServiceGroupUpsertModel Input { get; set; } = new();

    public IReadOnlyList<ServiceGroupAdminModel> ServiceGroups { get; private set; } = [];

    public async Task OnGetAsync()
    {
        ServiceGroups = await adminCatalogService.GetServiceGroupsAsync(HttpContext.RequestAborted);
        ViewData["Title"] = "Service Groups";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        await adminCatalogService.CreateServiceGroupAsync(Input, HttpContext.RequestAborted);
        return RedirectToPage();
    }
}
