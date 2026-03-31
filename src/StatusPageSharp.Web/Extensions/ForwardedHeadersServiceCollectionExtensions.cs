using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StatusPageSharp.Web.Configuration;

namespace StatusPageSharp.Web.Extensions;

public static class ForwardedHeadersServiceCollectionExtensions
{
    public static IServiceCollection AddStatusPageForwardedHeaders(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var settings =
            configuration
                .GetSection(StatusPageForwardedHeadersOptions.SectionName)
                .Get<StatusPageForwardedHeadersOptions>()
            ?? new StatusPageForwardedHeadersOptions();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedHost
                | ForwardedHeaders.XForwardedProto
                | ForwardedHeaders.XForwardedPrefix;

            if (settings.AllowAnyProxy)
            {
                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
                return;
            }

            ConfigureKnownProxies(options, settings.KnownProxies);
            ConfigureKnownNetworks(options, settings.KnownIPNetworks);
        });

        return services;
    }

    private static void ConfigureKnownProxies(
        ForwardedHeadersOptions options,
        IEnumerable<string> knownProxies
    )
    {
        foreach (var knownProxy in knownProxies)
        {
            if (!IPAddress.TryParse(knownProxy, out var address))
            {
                throw new InvalidOperationException(
                    $"Invalid forwarded headers proxy '{knownProxy}'."
                );
            }

            options.KnownProxies.Add(address);
        }
    }

    private static void ConfigureKnownNetworks(
        ForwardedHeadersOptions options,
        IEnumerable<string> knownNetworks
    )
    {
        foreach (var knownNetwork in knownNetworks)
        {
            try
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(knownNetwork));
            }
            catch (Exception exception) when (exception is ArgumentNullException or FormatException)
            {
                throw new InvalidOperationException(
                    $"Invalid forwarded headers network '{knownNetwork}'.",
                    exception
                );
            }
        }
    }
}
