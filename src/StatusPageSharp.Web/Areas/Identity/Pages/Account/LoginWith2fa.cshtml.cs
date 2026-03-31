using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StatusPageSharp.Infrastructure.Identity;

namespace StatusPageSharp.Web.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginWith2faModel(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    ILogger<LoginWith2faModel> logger
) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool RememberMe { get; private set; }

    public string? ReturnUrl { get; private set; }

    public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
    {
        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return NotFound("Unable to load two-factor authentication user.");
        }

        ReturnUrl = returnUrl;
        RememberMe = rememberMe;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool rememberMe, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ReturnUrl = returnUrl;
            RememberMe = rememberMe;
            return Page();
        }

        returnUrl ??= Url.Content("~/");

        var user = await signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user is null)
        {
            return NotFound("Unable to load two-factor authentication user.");
        }

        var authenticatorCode = Input
            .AuthenticatorCode.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);

        var result = await signInManager.TwoFactorAuthenticatorSignInAsync(
            authenticatorCode,
            rememberMe,
            Input.RememberMachine
        );

        var userId = await userManager.GetUserIdAsync(user);
        if (result.Succeeded)
        {
            logger.LogInformation("User with ID '{UserId}' logged in with 2fa.", userId);
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("User account locked out.");
            return RedirectToPage("./Lockout");
        }

        ModelState.AddModelError(
            $"{nameof(Input)}.{nameof(Input.AuthenticatorCode)}",
            "Invalid authenticator code."
        );
        ReturnUrl = returnUrl;
        RememberMe = rememberMe;
        return Page();
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "Authenticator code")]
        [StringLength(7, MinimumLength = 6)]
        [DataType(DataType.Text)]
        public string AuthenticatorCode { get; set; } = string.Empty;

        [Display(Name = "Remember this machine")]
        public bool RememberMachine { get; set; }
    }
}
