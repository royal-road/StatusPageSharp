using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StatusPageSharp.Domain.Entities;

namespace StatusPageSharp.Infrastructure.Data.Configurations;

public class ServiceGroupConfiguration : IEntityTypeConfiguration<ServiceGroup>
{
    public void Configure(EntityTypeBuilder<ServiceGroup> builder)
    {
        builder.Property(serviceGroup => serviceGroup.Name).HasMaxLength(120);
        builder.Property(serviceGroup => serviceGroup.Slug).HasMaxLength(120);
        builder.Property(serviceGroup => serviceGroup.Description).HasMaxLength(500);
        builder.HasIndex(serviceGroup => serviceGroup.Slug).IsUnique();
    }
}
