using Microsoft.EntityFrameworkCore;
using StatusPageSharp.Domain.Entities;
using StatusPageSharp.Infrastructure.Data;

namespace StatusPageSharp.Infrastructure.Services;

public static class DailyIncidentCountRollupSynchronizer
{
    public static async Task SyncRecentIncidentCountsAsync(
        ApplicationDbContext dbContext,
        Guid serviceId,
        DateTime now,
        CancellationToken cancellationToken
    )
    {
        var startDay = DateOnly.FromDateTime(now.Date.AddDays(-29));
        var endDay = DateOnly.FromDateTime(now.Date);
        var startUtc = DateTime.SpecifyKind(
            startDay.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc
        );
        var endUtc = DateTime.SpecifyKind(
            endDay.AddDays(1).ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Utc
        );
        var existingRollups = await dbContext
            .DailyServiceRollups.Where(item =>
                item.ServiceId == serviceId && item.Day >= startDay && item.Day <= endDay
            )
            .ToDictionaryAsync(item => item.Day, cancellationToken);

        foreach (
            var localRollup in dbContext.DailyServiceRollups.Local.Where(item =>
                item.ServiceId == serviceId && item.Day >= startDay && item.Day <= endDay
            )
        )
        {
            existingRollups[localRollup.Day] = localRollup;
        }

        var incidentWindows = (
            await dbContext
                .IncidentAffectedServices.AsNoTracking()
                .Where(item =>
                    item.ServiceId == serviceId
                    && item.AddedUtc < endUtc
                    && (item.ResolvedUtc ?? now) > startUtc
                )
                .Select(item => new
                {
                    item.Id,
                    item.IncidentId,
                    item.AddedUtc,
                    EndedUtc = item.ResolvedUtc ?? now,
                })
                .ToListAsync(cancellationToken)
        ).ToDictionary(item => item.Id);

        foreach (
            var trackedWindow in dbContext
                .ChangeTracker.Entries<IncidentAffectedService>()
                .Where(entry => entry.Entity.ServiceId == serviceId)
        )
        {
            if (trackedWindow.State == EntityState.Deleted)
            {
                incidentWindows.Remove(trackedWindow.Entity.Id);
                continue;
            }

            var endedUtc = trackedWindow.Entity.ResolvedUtc ?? now;
            if (trackedWindow.Entity.AddedUtc >= endUtc || endedUtc <= startUtc)
            {
                incidentWindows.Remove(trackedWindow.Entity.Id);
                continue;
            }

            incidentWindows[trackedWindow.Entity.Id] = new
            {
                trackedWindow.Entity.Id,
                trackedWindow.Entity.IncidentId,
                trackedWindow.Entity.AddedUtc,
                EndedUtc = endedUtc,
            };
        }

        Dictionary<DateOnly, HashSet<Guid>> incidentIdsByDay = [];
        foreach (var incidentWindow in incidentWindows.Values)
        {
            var firstDayNumber = Math.Max(
                startDay.DayNumber,
                DateOnly.FromDateTime(incidentWindow.AddedUtc).DayNumber
            );
            var lastDayNumber = Math.Min(
                endDay.DayNumber,
                DateOnly.FromDateTime(incidentWindow.EndedUtc.AddTicks(-1)).DayNumber
            );

            for (var dayNumber = firstDayNumber; dayNumber <= lastDayNumber; dayNumber++)
            {
                var day = DateOnly.FromDayNumber(dayNumber);
                if (!incidentIdsByDay.TryGetValue(day, out var incidentIds))
                {
                    incidentIds = [];
                    incidentIdsByDay[day] = incidentIds;
                }

                incidentIds.Add(incidentWindow.IncidentId);
            }
        }

        for (var dayNumber = startDay.DayNumber; dayNumber <= endDay.DayNumber; dayNumber++)
        {
            var day = DateOnly.FromDayNumber(dayNumber);
            var incidentCount = incidentIdsByDay.TryGetValue(day, out var incidentIds)
                ? incidentIds.Count
                : 0;

            if (existingRollups.TryGetValue(day, out var rollup))
            {
                rollup.IncidentCount = incidentCount;
                continue;
            }

            if (incidentCount == 0)
            {
                continue;
            }

            dbContext.DailyServiceRollups.Add(
                new DailyServiceRollup
                {
                    ServiceId = serviceId,
                    Day = day,
                    IncidentCount = incidentCount,
                }
            );
        }
    }
}
