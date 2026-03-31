using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace StatusPageSharp.Infrastructure.Identity;

public static class DesignTimeIdentityOptionsServiceCollectionExtensions
{
    public static IServiceCollection AddStatusPageDesignTimeIdentityOptions(
        this IServiceCollection services
    )
    {
        services.Configure<IdentityOptions>(StatusPageIdentityOptionsConfiguration.Configure);
        return services;
    }
}
