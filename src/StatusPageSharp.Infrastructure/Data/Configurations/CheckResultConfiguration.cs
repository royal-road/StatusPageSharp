using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StatusPageSharp.Domain.Entities;

namespace StatusPageSharp.Infrastructure.Data.Configurations;

public class CheckResultConfiguration : IEntityTypeConfiguration<CheckResult>
{
    public void Configure(EntityTypeBuilder<CheckResult> builder)
    {
        builder.Property(checkResult => checkResult.FailureMessage).HasMaxLength(500);
        builder.Property(checkResult => checkResult.ResponseSnippet).HasMaxLength(1000);
        builder.HasIndex(checkResult => new { checkResult.ServiceId, checkResult.StartedUtc });
    }
}
