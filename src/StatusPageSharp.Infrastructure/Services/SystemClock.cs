using StatusPageSharp.Application.Abstractions;

namespace StatusPageSharp.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
