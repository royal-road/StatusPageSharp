using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StatusPageSharp.Domain.Entities;
using StatusPageSharp.Infrastructure.Identity;

namespace StatusPageSharp.Infrastructure.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<ServiceGroup> ServiceGroups => Set<ServiceGroup>();

    public DbSet<Service> Services => Set<Service>();

    public DbSet<MonitorDefinition> MonitorDefinitions => Set<MonitorDefinition>();

    public DbSet<CheckResult> CheckResults => Set<CheckResult>();

    public DbSet<HourlyServiceRollup> HourlyServiceRollups => Set<HourlyServiceRollup>();

    public DbSet<DailyServiceRollup> DailyServiceRollups => Set<DailyServiceRollup>();

    public DbSet<MonthlySlaSnapshot> MonthlySlaSnapshots => Set<MonthlySlaSnapshot>();

    public DbSet<Incident> Incidents => Set<Incident>();

    public DbSet<IncidentAffectedService> IncidentAffectedServices =>
        Set<IncidentAffectedService>();

    public DbSet<IncidentEvent> IncidentEvents => Set<IncidentEvent>();

    public DbSet<ScheduledMaintenance> ScheduledMaintenances => Set<ScheduledMaintenance>();

    public DbSet<ScheduledMaintenanceService> ScheduledMaintenanceServices =>
        Set<ScheduledMaintenanceService>();

    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
