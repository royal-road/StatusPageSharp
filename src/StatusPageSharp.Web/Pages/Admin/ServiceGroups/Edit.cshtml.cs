using Microsoft.AspNetCore.Mvc;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;

namespace StatusPageSharp.Web.Pages.Admin.ServiceGroups;

public class EditModel(IAdminCatalogService adminCatalogService) : AdminPageModel
{
    [BindProperty]
    public ServiceGroupUpsertModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var groups = await adminCatalogService.GetServiceGroupsAsync(HttpContext.RequestAborted);
        var group = groups.SingleOrDefault(item => item.Id == id);
        if (group is null)
        {
            return NotFound();
        }

        Input = new ServiceGroupUpsertModel
        {
            Name = group.Name,
            Slug = group.Slug,
            Description = group.Description,
            DisplayOrder = group.DisplayOrder,
        };
        ViewData["Title"] = $"Edit {group.Name}";
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await adminCatalogService.UpdateServiceGroupAsync(id, Input, HttpContext.RequestAborted);
        return RedirectToPage("/Admin/ServiceGroups/Index");
    }
}
