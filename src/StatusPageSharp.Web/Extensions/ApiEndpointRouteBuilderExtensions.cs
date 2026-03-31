using StatusPageSharp.Application.Abstractions;
using StatusPageSharp.Web.Caching;
using StatusPageSharp.Web.Rendering;

namespace StatusPageSharp.Web.Extensions;

public static class ApiEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapStatusPageApis(
        this IEndpointRouteBuilder endpointRouteBuilder
    )
    {
        var publicApi = endpointRouteBuilder.MapGroup("/api/public");
        publicApi.MapGet(
            "/status",
            async (IPublicStatusService publicStatusService, CancellationToken cancellationToken) =>
                Results.Ok(await publicStatusService.GetSiteSummaryAsync(cancellationToken))
        );
        publicApi.MapGet(
            "/services/{slug}/history",
            async (
                string slug,
                IPublicStatusService publicStatusService,
                CancellationToken cancellationToken
            ) =>
            {
                var history = await publicStatusService.GetServiceHistoryAsync(
                    slug,
                    cancellationToken
                );
                return history is null ? Results.NotFound() : Results.Ok(history);
            }
        );
        publicApi.MapGet(
            "/incidents",
            async (
                IIncidentManagementService incidentManagementService,
                CancellationToken cancellationToken
            ) =>
                Results.Ok(
                    await incidentManagementService.GetIncidentSummariesAsync(cancellationToken)
                )
        );
        publicApi.MapGet(
            "/incidents/{id:guid}",
            async (
                Guid id,
                IIncidentManagementService incidentManagementService,
                CancellationToken cancellationToken
            ) =>
            {
                var incident = await incidentManagementService.GetIncidentAsync(
                    id,
                    cancellationToken
                );
                return incident is null ? Results.NotFound() : Results.Ok(incident);
            }
        );
        publicApi
            .MapGet(
                "/status-card.png",
                async (
                    HttpContext httpContext,
                    IPublicStatusService publicStatusService,
                    CancellationToken cancellationToken
                ) =>
                {
                    StatusCardResponseCacheHeaders.Apply(httpContext.Response.Headers);
                    return TypedResults.File(
                        SocialStatusCardRenderer.Render(
                            await publicStatusService.GetSiteSummaryAsync(cancellationToken)
                        ),
                        "image/png"
                    );
                }
            )
            .CacheOutput("StatusCard");

        var adminApi = endpointRouteBuilder
            .MapGroup("/api/admin")
            .RequireAuthorization("AdminOnly");
        adminApi.MapGet(
            "/services",
            async (IAdminCatalogService adminCatalogService, CancellationToken cancellationToken) =>
                Results.Ok(await adminCatalogService.GetServicesAsync(cancellationToken))
        );
        adminApi.MapGet(
            "/service-groups",
            async (IAdminCatalogService adminCatalogService, CancellationToken cancellationToken) =>
                Results.Ok(await adminCatalogService.GetServiceGroupsAsync(cancellationToken))
        );
        adminApi.MapGet(
            "/incidents",
            async (
                IIncidentManagementService incidentManagementService,
                CancellationToken cancellationToken
            ) =>
                Results.Ok(
                    await incidentManagementService.GetAdminIncidentsAsync(cancellationToken)
                )
        );
        adminApi.MapGet(
            "/maintenance",
            async (
                IMaintenanceManagementService maintenanceManagementService,
                CancellationToken cancellationToken
            ) =>
                Results.Ok(
                    await maintenanceManagementService.GetAdminMaintenanceAsync(cancellationToken)
                )
        );
        adminApi.MapGet(
            "/users",
            async (
                IUserManagementService userManagementService,
                CancellationToken cancellationToken
            ) => Results.Ok(await userManagementService.GetUsersAsync(cancellationToken))
        );
        adminApi.MapGet(
            "/settings",
            async (ISiteSettingsService siteSettingsService, CancellationToken cancellationToken) =>
                Results.Ok(await siteSettingsService.GetSettingsAsync(cancellationToken))
        );

        return endpointRouteBuilder;
    }
}
