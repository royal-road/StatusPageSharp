using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StatusPageSharp.Domain.Entities;

namespace StatusPageSharp.Infrastructure.Data.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.Property(service => service.Name).HasMaxLength(120);
        builder.Property(service => service.Slug).HasMaxLength(120);
        builder.Property(service => service.Description).HasMaxLength(500);
        builder.Property(service => service.LastFailureMessage).HasMaxLength(500);
        builder.HasIndex(service => service.Slug).IsUnique();
        builder.HasIndex(service => new { service.IsEnabled, service.NextCheckUtc });
        builder
            .HasOne(service => service.MonitorDefinition)
            .WithOne(monitorDefinition => monitorDefinition.Service)
            .HasForeignKey<MonitorDefinition>(monitorDefinition => monitorDefinition.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
