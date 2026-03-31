using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StatusPageSharp.Web.Pages.Admin;

public abstract class AdminPageModel : PageModel
{
    protected string? CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier);
}
