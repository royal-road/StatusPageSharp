using Microsoft.AspNetCore.Mvc.RazorPages;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Web.Metadata;

namespace StatusPageSharp.Web.Pages;

public class IndexModel(IPublicStatusService publicStatusService) : PageModel
{
    public PublicSiteSummaryModel SiteSummary { get; private set; } = null!;

    public async Task OnGetAsync()
    {
        SiteSummary = await publicStatusService.GetSiteSummaryAsync(HttpContext.RequestAborted);
        ViewData["Title"] = "System Status";
        ViewData["Description"] = SocialMetadataBuilder.BuildSiteDescription(SiteSummary);
    }
}
