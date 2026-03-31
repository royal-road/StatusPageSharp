using Microsoft.AspNetCore.Mvc;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Exceptions;
using StatusPageSharp.Application.Models.Admin;

namespace StatusPageSharp.Web.Pages.Admin.Users;

public class IndexModel(IUserManagementService userManagementService) : AdminPageModel
{
    public UserCreateModel NewUser { get; set; } = new();

    public UserPasswordResetModel PasswordReset { get; set; } = new();

    public IReadOnlyList<UserAdminModel> Users { get; private set; } = [];

    public Task OnGetAsync() => LoadAsync();

    public async Task<IActionResult> OnPostCreateAsync(UserCreateModel newUser)
    {
        NewUser = newUser;
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        try
        {
            await userManagementService.CreateUserAsync(newUser, HttpContext.RequestAborted);
        }
        catch (UserManagementException exception)
        {
            AddErrors(exception);
            await LoadAsync();
            return Page();
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(string id, bool isEnabled)
    {
        await userManagementService.SetEnabledAsync(id, isEnabled, HttpContext.RequestAborted);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(UserPasswordResetModel passwordReset)
    {
        PasswordReset = passwordReset;
        if (!ModelState.IsValid)
        {
            await LoadAsync();
            return Page();
        }

        try
        {
            await userManagementService.ResetPasswordAsync(
                passwordReset,
                HttpContext.RequestAborted
            );
        }
        catch (UserManagementException exception)
        {
            AddErrors(exception);
            await LoadAsync();
            return Page();
        }

        return RedirectToPage();
    }

    private void AddErrors(UserManagementException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(string.Empty, error);
        }
    }

    private Task LoadAsync() => LoadAsync(HttpContext.RequestAborted);

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Users = await userManagementService.GetUsersAsync(cancellationToken);
        ViewData["Title"] = "Users";
    }
}
