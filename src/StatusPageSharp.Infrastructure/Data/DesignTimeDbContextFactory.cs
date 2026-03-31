using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using StatusPageSharp.Infrastructure.Identity;

namespace StatusPageSharp.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("STATUSPAGESHARP_CONNECTION_STRING")
            ?? "Host=localhost;Database=statuspagesharp;Username=postgres;Password=postgres";

        var services = new ServiceCollection();
        services.AddStatusPageDesignTimeIdentityOptions();
        var serviceProvider = services.BuildServiceProvider();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            options => options.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
        );
        optionsBuilder.UseApplicationServiceProvider(serviceProvider);
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
