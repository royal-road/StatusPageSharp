using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StatusPageSharp.Domain.Entities;

namespace StatusPageSharp.Infrastructure.Data.Configurations;

public class HourlyServiceRollupConfiguration : IEntityTypeConfiguration<HourlyServiceRollup>
{
    public void Configure(EntityTypeBuilder<HourlyServiceRollup> builder)
    {
        builder.HasIndex(rollup => new { rollup.ServiceId, rollup.HourStartUtc }).IsUnique();
    }
}
