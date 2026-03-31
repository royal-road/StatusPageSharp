using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StatusPageSharp.Domain.Entities;

namespace StatusPageSharp.Infrastructure.Data.Configurations;

public class IncidentEventConfiguration : IEntityTypeConfiguration<IncidentEvent>
{
    public void Configure(EntityTypeBuilder<IncidentEvent> builder)
    {
        builder.Property(incidentEvent => incidentEvent.Message).HasMaxLength(120);
        builder.Property(incidentEvent => incidentEvent.Body).HasColumnType("text");
        builder.HasIndex(incidentEvent => new
        {
            incidentEvent.IncidentId,
            incidentEvent.CreatedUtc,
        });
    }
}
