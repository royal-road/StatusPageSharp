using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StatusPageSharp.Domain.Entities;

namespace StatusPageSharp.Infrastructure.Data.Configurations;

public class ScheduledMaintenanceServiceConfiguration
    : IEntityTypeConfiguration<ScheduledMaintenanceService>
{
    public void Configure(EntityTypeBuilder<ScheduledMaintenanceService> builder)
    {
        builder.HasIndex(link => new { link.ScheduledMaintenanceId, link.ServiceId }).IsUnique();
    }
}
