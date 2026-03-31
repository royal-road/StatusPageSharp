using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Public;

namespace StatusPageSharp.Web.Pages.Services;

public class DetailsModel(IPublicStatusService publicStatusService) : PageModel
{
    public PublicServiceDetailsModel Service { get; private set; } = null!;

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        var service = await publicStatusService.GetServiceDetailsAsync(
            slug,
            HttpContext.RequestAborted
        );
        if (service is null)
        {
            return NotFound();
        }

        Service = service;
        ViewData["Title"] = service.Name;
        return Page();
    }
}
