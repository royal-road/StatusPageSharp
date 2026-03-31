using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Domain.Constants;
using StatusPageSharp.Infrastructure.Identity;

namespace StatusPageSharp.Infrastructure.Services;

public sealed class UserManagementService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IClock clock
) : IUserManagementService
{
    public async Task<IReadOnlyList<UserAdminModel>> GetUsersAsync(
        CancellationToken cancellationToken
    )
    {
        return await userManager
            .Users.OrderBy(user => user.Email)
            .Select(user => new UserAdminModel(
                user.Id,
                user.Email!,
                user.DisplayName,
                user.EmailConfirmed,
                user.IsEnabled,
                user.CreatedUtc
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task CreateUserAsync(UserCreateModel model, CancellationToken cancellationToken)
    {
        if (!await roleManager.RoleExistsAsync(RoleNames.Administrator))
        {
            await roleManager.CreateAsync(new IdentityRole(RoleNames.Administrator));
        }

        var user = new ApplicationUser
        {
            UserName = model.Email.Trim(),
            Email = model.Email.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(model.DisplayName)
                ? null
                : model.DisplayName.Trim(),
            EmailConfirmed = true,
            IsEnabled = true,
            CreatedUtc = clock.UtcNow,
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(error => error.Description))
            );
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, RoleNames.Administrator);
        if (!addRoleResult.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", addRoleResult.Errors.Select(error => error.Description))
            );
        }
    }

    public async Task SetEnabledAsync(
        string userId,
        bool isEnabled,
        CancellationToken cancellationToken
    )
    {
        var user = await userManager.Users.SingleAsync(
            item => item.Id == userId,
            cancellationToken
        );
        user.IsEnabled = isEnabled;
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(error => error.Description))
            );
        }

        await userManager.UpdateSecurityStampAsync(user);
    }

    public async Task ResetPasswordAsync(
        UserPasswordResetModel model,
        CancellationToken cancellationToken
    )
    {
        var user = await userManager.Users.SingleAsync(
            item => item.Id == model.UserId,
            cancellationToken
        );
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", result.Errors.Select(error => error.Description))
            );
        }
    }
}
