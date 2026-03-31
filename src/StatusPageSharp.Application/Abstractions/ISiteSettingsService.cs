using StatusPageSharp.Application.Models.Admin;

namespace StatusPageSharp.Application.Abstractions;

public interface ISiteSettingsService
{
    Task<SiteSettingsAdminModel> GetSettingsAsync(CancellationToken cancellationToken);

    Task UpdateSettingsAsync(SiteSettingsAdminModel model, CancellationToken cancellationToken);
}
