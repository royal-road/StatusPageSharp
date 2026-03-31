using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Infrastructure.Identity;

namespace StatusPageSharp.Web.Areas.Identity.Pages.Account.Manage;

public class EnableAuthenticatorModel(
    UserManager<ApplicationUser> userManager,
    ILogger<EnableAuthenticatorModel> logger,
    UrlEncoder urlEncoder,
    ISiteSettingsService siteSettingsService
) : PageModel
{
    private const string AuthenticatorUriFormat =
        "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

    public string SharedKey { get; private set; } = string.Empty;

    public string AuthenticatorUri { get; private set; } = string.Empty;

    public string QrCodeSvg { get; private set; } = string.Empty;

    [TempData]
    public string[]? RecoveryCodes { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        await LoadAsync(user, HttpContext.RequestAborted);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user, HttpContext.RequestAborted);
            return Page();
        }

        var verificationCode = Input.Code.Replace(" ", string.Empty).Replace("-", string.Empty);
        var isValid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            userManager.Options.Tokens.AuthenticatorTokenProvider,
            verificationCode
        );

        if (!isValid)
        {
            ModelState.AddModelError("Input.Code", "Verification code is invalid.");
            await LoadAsync(user, HttpContext.RequestAborted);
            return Page();
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);
        var userId = await userManager.GetUserIdAsync(user);
        logger.LogInformation(
            "User with ID '{UserId}' has enabled 2FA with an authenticator app.",
            userId
        );

        StatusMessage = "Your authenticator app has been verified.";

        if (await userManager.CountRecoveryCodesAsync(user) == 0)
        {
            var recoveryCodes = await userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            RecoveryCodes =
            [
                .. (
                    recoveryCodes
                    ?? throw new InvalidOperationException("Unable to generate recovery codes.")
                ),
            ];
            return RedirectToPage("./GenerateRecoveryCodes");
        }

        return RedirectToPage("./TwoFactorAuthentication");
    }

    private async Task LoadAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(unformattedKey))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            unformattedKey = await userManager.GetAuthenticatorKeyAsync(user);
        }

        if (string.IsNullOrWhiteSpace(unformattedKey))
        {
            throw new InvalidOperationException("Unable to load authenticator key.");
        }

        SharedKey = FormatKey(unformattedKey);

        var siteSettings = await siteSettingsService.GetSettingsAsync(cancellationToken);
        var email =
            await userManager.GetEmailAsync(user)
            ?? throw new InvalidOperationException("Unable to load email for authenticator setup.");
        AuthenticatorUri = string.Format(
            CultureInfo.InvariantCulture,
            AuthenticatorUriFormat,
            urlEncoder.Encode(siteSettings.SiteTitle),
            urlEncoder.Encode(email),
            unformattedKey
        );
        QrCodeSvg = CreateQrCodeSvg(AuthenticatorUri);
    }

    private static string CreateQrCodeSvg(string authenticatorUri)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(
            authenticatorUri,
            QRCodeGenerator.ECCLevel.Q
        );
        using var svgQrCode = new SvgQRCode(qrCodeData);

        return svgQrCode
            .GetGraphic(12)
            .Replace("<svg ", "<svg class=\"block h-full w-full\" ", StringComparison.Ordinal);
    }

    private static string FormatKey(string unformattedKey)
    {
        var builder = new StringBuilder();
        var position = 0;
        while (position + 4 < unformattedKey.Length)
        {
            builder.Append(unformattedKey.AsSpan(position, 4)).Append(' ');
            position += 4;
        }

        if (position < unformattedKey.Length)
        {
            builder.Append(unformattedKey.AsSpan(position));
        }

        return builder.ToString().ToLowerInvariant();
    }

    public class InputModel
    {
        [Required]
        [StringLength(7, MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Verification code")]
        public string Code { get; set; } = string.Empty;
    }
}
