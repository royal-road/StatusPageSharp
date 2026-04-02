using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Infrastructure.Configuration;
using StatusPageSharp.Infrastructure.Data;
using StatusPageSharp.Infrastructure.Monitoring;
using StatusPageSharp.Infrastructure.Services;

namespace StatusPageSharp.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddStatusPageInfrastructureForWeb(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            ConfigureDatabase(options, configuration)
        );
        AddStatusPageApplicationServices(services);
        return services;
    }

    public static IServiceCollection AddStatusPageInfrastructureForWorker(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContextFactory<ApplicationDbContext>(options =>
            ConfigureDatabase(options, configuration)
        );
        AddStatusPageMonitoringServices(services);
        return services;
    }

    public static IServiceCollection AddStatusPageBootstrapOptions(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.Configure<BootstrapAdminOptions>(
            configuration.GetSection(BootstrapAdminOptions.SectionName)
        );
        return services;
    }

    private static void AddStatusPageApplicationServices(this IServiceCollection services)
    {
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<IPublicStatusService, PublicStatusService>();
        services.AddScoped<IAdminCatalogService, AdminCatalogService>();
        services.AddScoped<IIncidentManagementService, IncidentManagementService>();
        services.AddScoped<IMaintenanceManagementService, MaintenanceManagementService>();
        services.AddScoped<ISiteSettingsService, SiteSettingsService>();
        services.AddScoped<IUserManagementService, UserManagementService>();
    }

    private static void AddStatusPageMonitoringServices(this IServiceCollection services)
    {
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddHttpClient();
        services.AddTransient<MonitorProbeClient>();
        services.AddSingleton<IMonitoringCoordinator, MonitoringCoordinator>();
    }

    private static void ConfigureDatabase(
        DbContextOptionsBuilder optionsBuilder,
        IConfiguration configuration
    )
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found."
            );

        optionsBuilder.UseNpgsql(
            connectionString,
            options =>
                options
                    .MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
                    .EnableRetryOnFailure()
        );
    }
}
