using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StatusPageSharp.Domain.Entities;

namespace StatusPageSharp.Infrastructure.Data.Configurations;

public class IncidentAffectedServiceConfiguration
    : IEntityTypeConfiguration<IncidentAffectedService>
{
    public void Configure(EntityTypeBuilder<IncidentAffectedService> builder)
    {
        builder.HasIndex(affectedService => new
        {
            affectedService.IncidentId,
            affectedService.ServiceId,
            affectedService.IsResolved,
        });
    }
}
