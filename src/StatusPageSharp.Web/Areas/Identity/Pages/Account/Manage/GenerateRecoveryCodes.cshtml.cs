using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StatusPageSharp.Infrastructure.Identity;

namespace StatusPageSharp.Web.Areas.Identity.Pages.Account.Manage;

public class GenerateRecoveryCodesModel(
    UserManager<ApplicationUser> userManager,
    ILogger<GenerateRecoveryCodesModel> logger
) : PageModel
{
    [TempData]
    public string[] RecoveryCodes { get; set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        if (!await userManager.GetTwoFactorEnabledAsync(user))
        {
            throw new InvalidOperationException(
                "Cannot generate recovery codes for a user without 2FA enabled."
            );
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        if (!await userManager.GetTwoFactorEnabledAsync(user))
        {
            throw new InvalidOperationException(
                "Cannot generate recovery codes for a user without 2FA enabled."
            );
        }

        var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        RecoveryCodes =
        [
            .. (
                recoveryCodes
                ?? throw new InvalidOperationException("Unable to generate recovery codes.")
            ),
        ];
        logger.LogInformation(
            "User with ID '{UserId}' has generated new 2FA recovery codes.",
            await userManager.GetUserIdAsync(user)
        );
        StatusMessage = "New recovery codes generated.";
        return RedirectToPage();
    }
}
