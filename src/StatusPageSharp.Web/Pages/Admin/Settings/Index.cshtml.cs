using Microsoft.AspNetCore.Mvc;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;

namespace StatusPageSharp.Web.Pages.Admin.Settings;

public class IndexModel(ISiteSettingsService siteSettingsService) : AdminPageModel
{
    [BindProperty]
    public SiteSettingsAdminModel Input { get; set; } = new();

    public async Task OnGetAsync()
    {
        Input = await siteSettingsService.GetSettingsAsync(HttpContext.RequestAborted);
        ViewData["Title"] = "Site Settings";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        await siteSettingsService.UpdateSettingsAsync(Input, HttpContext.RequestAborted);
        return RedirectToPage();
    }
}
