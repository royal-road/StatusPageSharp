using Microsoft.AspNetCore.Mvc;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Domain.Enums;

namespace StatusPageSharp.Web.Pages.Admin.Services;

public class CreateModel(IAdminCatalogService adminCatalogService) : AdminPageModel
{
    [BindProperty]
    public ServiceUpsertModel Input { get; set; } = new() { MonitorType = MonitorType.Https };

    public IReadOnlyList<ServiceGroupAdminModel> Groups { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Groups = await adminCatalogService.GetServiceGroupsAsync(HttpContext.RequestAborted);
        ViewData["Title"] = "Create Service";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Groups = await adminCatalogService.GetServiceGroupsAsync(HttpContext.RequestAborted);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await adminCatalogService.CreateServiceAsync(Input, HttpContext.RequestAborted);
        return RedirectToPage("/Admin/Services/Index");
    }
}
