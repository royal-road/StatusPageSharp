using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StatusPageSharp.Domain.Constants;
using StatusPageSharp.Domain.Entities;
using StatusPageSharp.Infrastructure.Configuration;
using StatusPageSharp.Infrastructure.Data;
using StatusPageSharp.Infrastructure.Identity;

namespace StatusPageSharp.Infrastructure.Setup;

public static class ApplicationSetupExtensions
{
    public static async Task InitializeStatusPageAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default
    )
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        if (!await dbContext.SiteSettings.AnyAsync(cancellationToken))
        {
            dbContext.SiteSettings.Add(
                new SiteSetting
                {
                    Id = 1,
                    SiteTitle = "StatusPageSharp",
                    PublicRefreshIntervalSeconds = 60,
                    DefaultRawRetentionDays = 30,
                }
            );
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(RoleNames.Administrator))
        {
            await roleManager.CreateAsync(new IdentityRole(RoleNames.Administrator));
        }

        var bootstrapOptions = scope
            .ServiceProvider.GetRequiredService<IOptions<BootstrapAdminOptions>>()
            .Value;
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (bootstrapOptions.Enabled && !await userManager.Users.AnyAsync(cancellationToken))
        {
            if (
                string.IsNullOrWhiteSpace(bootstrapOptions.Email)
                || string.IsNullOrWhiteSpace(bootstrapOptions.Password)
            )
            {
                throw new InvalidOperationException(
                    "BootstrapAdmin requires Email and Password when enabled."
                );
            }

            var user = new ApplicationUser
            {
                UserName = bootstrapOptions.Email,
                Email = bootstrapOptions.Email,
                DisplayName = bootstrapOptions.DisplayName,
                EmailConfirmed = true,
                CreatedUtc = DateTime.UtcNow,
                IsEnabled = true,
            };

            var createResult = await userManager.CreateAsync(user, bootstrapOptions.Password);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ", createResult.Errors.Select(error => error.Description))
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

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
