using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StatusPageSharp.Domain.Entities;

namespace StatusPageSharp.Infrastructure.Data.Configurations;

public class MonitorDefinitionConfiguration : IEntityTypeConfiguration<MonitorDefinition>
{
    public void Configure(EntityTypeBuilder<MonitorDefinition> builder)
    {
        builder.Property(monitorDefinition => monitorDefinition.Host).HasMaxLength(255);
        builder.Property(monitorDefinition => monitorDefinition.Url).HasMaxLength(2000);
        builder.Property(monitorDefinition => monitorDefinition.HttpMethod).HasMaxLength(20);
        builder.Property(monitorDefinition => monitorDefinition.RequestBody).HasColumnType("text");
        builder
            .Property(monitorDefinition => monitorDefinition.RequestHeadersJson)
            .HasColumnType("text");
        builder
            .Property(monitorDefinition => monitorDefinition.ExpectedStatusCodes)
            .HasMaxLength(80);
        builder
            .Property(monitorDefinition => monitorDefinition.ExpectedResponseSubstring)
            .HasMaxLength(500);
        builder.HasIndex(monitorDefinition => monitorDefinition.ServiceId).IsUnique();
    }
}
