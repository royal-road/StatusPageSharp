using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StatusPageSharp.Infrastructure.Identity;

namespace StatusPageSharp.Web.Areas.Identity.Pages.Account.Manage;

public class TwoFactorAuthenticationModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager
) : PageModel
{
    public bool HasAuthenticator { get; private set; }

    public bool Is2FaEnabled { get; private set; }

    public bool IsMachineRemembered { get; private set; }

    public int RecoveryCodesLeft { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostForgetBrowserAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        await signInManager.ForgetTwoFactorClientAsync();
        StatusMessage = "The current browser has been forgotten.";
        return RedirectToPage();
    }

    private async Task LoadAsync(ApplicationUser user)
    {
        HasAuthenticator = !string.IsNullOrWhiteSpace(
            await userManager.GetAuthenticatorKeyAsync(user)
        );
        Is2FaEnabled = await userManager.GetTwoFactorEnabledAsync(user);
        IsMachineRemembered = await signInManager.IsTwoFactorClientRememberedAsync(user);
        RecoveryCodesLeft = await userManager.CountRecoveryCodesAsync(user);
    }
}
