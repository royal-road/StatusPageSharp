using StatusPageSharp.Application.Models.Admin;
using StatusPageSharp.Application.Models.Public;
using StatusPageSharp.Domain.Entities;

namespace StatusPageSharp.Infrastructure.Services;

public static class IncidentProjectionMapper
{
    public static IReadOnlyList<IncidentAffectedServiceAdminModel> MapAdminAffectedServices(
        IEnumerable<IncidentAffectedService> affectedServices
    ) =>
        GroupAffectedServices(affectedServices)
            .Select(group => new IncidentAffectedServiceAdminModel(
                group.ServiceId,
                group.ServiceName,
                group.ImpactLevel,
                group.AddedUtc,
                group.IsResolved,
                group.ResolvedUtc
            ))
            .ToList();

    public static IReadOnlyList<PublicIncidentServiceModel> MapPublicAffectedServices(
        IEnumerable<IncidentAffectedService> affectedServices
    ) =>
        GroupAffectedServices(affectedServices)
            .Select(group => new PublicIncidentServiceModel(
                group.ServiceId,
                group.ServiceName,
                group.ImpactLevel,
                group.IsResolved,
                group.ResolvedUtc
            ))
            .ToList();

    private static IEnumerable<GroupedIncidentAffectedService> GroupAffectedServices(
        IEnumerable<IncidentAffectedService> affectedServices
    ) =>
        affectedServices
            .GroupBy(item => new { item.ServiceId, item.Service.Name })
            .OrderBy(group => group.Key.Name)
            .Select(group =>
            {
                var activeWindow = group
                    .Where(item => !item.IsResolved)
                    .OrderByDescending(item => item.AddedUtc)
                    .FirstOrDefault();

                return new GroupedIncidentAffectedService(
                    group.Key.ServiceId,
                    group.Key.Name,
                    activeWindow?.ImpactLevel ?? group.Max(item => item.ImpactLevel),
                    activeWindow?.AddedUtc ?? group.Min(item => item.AddedUtc),
                    activeWindow is null,
                    activeWindow is null ? group.Max(item => item.ResolvedUtc) : null
                );
            });

    private sealed record GroupedIncidentAffectedService(
        Guid ServiceId,
        string ServiceName,
        Domain.Enums.IncidentImpactLevel ImpactLevel,
        DateTime AddedUtc,
        bool IsResolved,
        DateTime? ResolvedUtc
    );
}
