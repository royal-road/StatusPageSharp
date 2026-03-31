using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StatusPageSharp.Infrastructure.Identity;

namespace StatusPageSharp.Web.Areas.Identity.Pages.Account.Manage;

public class ResetAuthenticatorModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    ILogger<ResetAuthenticatorModel> logger
) : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        return user is null
            ? NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.")
            : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);
        await signInManager.RefreshSignInAsync(user);

        logger.LogInformation(
            "User with ID '{UserId}' has reset their authenticator app key.",
            await userManager.GetUserIdAsync(user)
        );

        StatusMessage =
            "Your authenticator app key has been reset. Configure your app again with the new key.";
        return RedirectToPage("./EnableAuthenticator");
    }
}
