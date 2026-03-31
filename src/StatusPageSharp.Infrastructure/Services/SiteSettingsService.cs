using Microsoft.EntityFrameworkCore;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Domain.Entities;
using StatusPageSharp.Infrastructure.Data;

namespace StatusPageSharp.Infrastructure.Services;

public sealed class SiteSettingsService(ApplicationDbContext dbContext) : ISiteSettingsService
{
    public async Task<SiteSettingsAdminModel> GetSettingsAsync(CancellationToken cancellationToken)
    {
        var siteSetting = await GetOrCreateSiteSettingAsync(cancellationToken);
        return new SiteSettingsAdminModel
        {
            SiteTitle = siteSetting.SiteTitle,
            LogoUrl = siteSetting.LogoUrl,
            PublicRefreshIntervalSeconds = siteSetting.PublicRefreshIntervalSeconds,
            DefaultRawRetentionDays = siteSetting.DefaultRawRetentionDays,
        };
    }

    public async Task UpdateSettingsAsync(
        SiteSettingsAdminModel model,
        CancellationToken cancellationToken
    )
    {
        var siteSetting = await GetOrCreateSiteSettingAsync(cancellationToken);
        siteSetting.SiteTitle = model.SiteTitle.Trim();
        siteSetting.LogoUrl = string.IsNullOrWhiteSpace(model.LogoUrl)
            ? null
            : model.LogoUrl.Trim();
        siteSetting.PublicRefreshIntervalSeconds = model.PublicRefreshIntervalSeconds;
        siteSetting.DefaultRawRetentionDays = model.DefaultRawRetentionDays;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<SiteSetting> GetOrCreateSiteSettingAsync(CancellationToken cancellationToken)
    {
        var siteSetting = await dbContext.SiteSettings.SingleOrDefaultAsync(
            item => item.Id == 1,
            cancellationToken
        );
        if (siteSetting is not null)
        {
            return siteSetting;
        }

        siteSetting = new SiteSetting
        {
            Id = 1,
            SiteTitle = "StatusPageSharp",
            PublicRefreshIntervalSeconds = 60,
            DefaultRawRetentionDays = 30,
        };

        dbContext.SiteSettings.Add(siteSetting);
        await dbContext.SaveChangesAsync(cancellationToken);
        return siteSetting;
    }
}
