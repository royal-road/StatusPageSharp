using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StatusPageSharp.Domain.Entities;

namespace StatusPageSharp.Infrastructure.Data.Configurations;

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.Property(siteSetting => siteSetting.SiteTitle).HasMaxLength(120);
        builder.Property(siteSetting => siteSetting.LogoUrl).HasMaxLength(2000);
    }
}
