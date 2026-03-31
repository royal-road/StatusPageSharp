using StatusPageSharp.Application.Models.Admin;

namespace StatusPageSharp.Application.Abstractions;

public interface IUserManagementService
{
    Task<IReadOnlyList<UserAdminModel>> GetUsersAsync(CancellationToken cancellationToken);

    Task CreateUserAsync(UserCreateModel model, CancellationToken cancellationToken);

    Task SetEnabledAsync(string userId, bool isEnabled, CancellationToken cancellationToken);

    Task ResetPasswordAsync(UserPasswordResetModel model, CancellationToken cancellationToken);
}
