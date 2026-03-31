using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StatusPageSharp.Domain.Entities;

namespace StatusPageSharp.Infrastructure.Data.Configurations;

public class MonthlySlaSnapshotConfiguration : IEntityTypeConfiguration<MonthlySlaSnapshot>
{
    public void Configure(EntityTypeBuilder<MonthlySlaSnapshot> builder)
    {
        builder.HasIndex(snapshot => new { snapshot.ServiceId, snapshot.Month }).IsUnique();
        builder.Property(snapshot => snapshot.UptimePercentage).HasPrecision(5, 2);
    }
}
