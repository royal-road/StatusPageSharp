using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StatusPageSharp.Domain.Entities;

namespace StatusPageSharp.Infrastructure.Data.Configurations;

public class IncidentConfiguration : IEntityTypeConfiguration<Incident>
{
    public void Configure(EntityTypeBuilder<Incident> builder)
    {
        builder.Property(incident => incident.Title).HasMaxLength(120);
        builder.Property(incident => incident.Summary).HasMaxLength(1000);
        builder.Property(incident => incident.Postmortem).HasColumnType("text");
        builder
            .HasMany(incident => incident.AffectedServices)
            .WithOne(affectedService => affectedService.Incident)
            .HasForeignKey(affectedService => affectedService.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder
            .HasMany(incident => incident.Events)
            .WithOne(incidentEvent => incidentEvent.Incident)
            .HasForeignKey(incidentEvent => incidentEvent.IncidentId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(incident => new { incident.Status, incident.StartedUtc });
        builder.HasIndex(incident => incident.Status).HasFilter("\"Status\" = 0").IsUnique();
    }
}
