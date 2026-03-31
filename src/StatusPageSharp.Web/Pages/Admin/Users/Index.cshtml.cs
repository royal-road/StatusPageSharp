using Microsoft.AspNetCore.Mvc;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;

namespace StatusPageSharp.Web.Pages.Admin.Users;

public class IndexModel(IUserManagementService userManagementService) : AdminPageModel
{
    [BindProperty]
    public UserCreateModel NewUser { get; set; } = new();

    [BindProperty]
    public UserPasswordResetModel PasswordReset { get; set; } = new();

    public IReadOnlyList<UserAdminModel> Users { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Users = await userManagementService.GetUsersAsync(HttpContext.RequestAborted);
        ViewData["Title"] = "Users";
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        ModelState.Remove($"{nameof(PasswordReset)}.{nameof(PasswordReset.UserId)}");
        ModelState.Remove($"{nameof(PasswordReset)}.{nameof(PasswordReset.NewPassword)}");
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        await userManagementService.CreateUserAsync(NewUser, HttpContext.RequestAborted);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(string id, bool isEnabled)
    {
        await userManagementService.SetEnabledAsync(id, isEnabled, HttpContext.RequestAborted);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync()
    {
        ModelState.Remove($"{nameof(NewUser)}.{nameof(NewUser.Email)}");
        ModelState.Remove($"{nameof(NewUser)}.{nameof(NewUser.DisplayName)}");
        ModelState.Remove($"{nameof(NewUser)}.{nameof(NewUser.Password)}");
        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        await userManagementService.ResetPasswordAsync(PasswordReset, HttpContext.RequestAborted);
        return RedirectToPage();
    }
}
