using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StatusPageSharp.Domain.Entities;

namespace StatusPageSharp.Infrastructure.Data.Configurations;

public class ScheduledMaintenanceConfiguration : IEntityTypeConfiguration<ScheduledMaintenance>
{
    public void Configure(EntityTypeBuilder<ScheduledMaintenance> builder)
    {
        builder.Property(maintenance => maintenance.Title).HasMaxLength(120);
        builder.Property(maintenance => maintenance.Summary).HasMaxLength(1000);
        builder.HasIndex(maintenance => new { maintenance.StartsUtc, maintenance.EndsUtc });
    }
}
