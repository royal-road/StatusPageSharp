using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StatusPageSharp.Domain.Entities;

namespace StatusPageSharp.Infrastructure.Data.Configurations;

public class DailyServiceRollupConfiguration : IEntityTypeConfiguration<DailyServiceRollup>
{
    public void Configure(EntityTypeBuilder<DailyServiceRollup> builder)
    {
        builder.HasIndex(rollup => new { rollup.ServiceId, rollup.Day }).IsUnique();
    }
}
