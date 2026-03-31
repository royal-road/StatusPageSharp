using StatusPageSharp.Application.Abstractions;

namespace StatusPageSharp.Infrastructure.Tests.Support;

public sealed class TestClock(DateTime utcNow) : IClock
{
    public DateTime UtcNow { get; } = utcNow;
}
