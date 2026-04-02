namespace StatusPageSharp.Infrastructure.Tests.Support;

public sealed class TestTimeProvider(DateTime utcNow) : TimeProvider
{
    private readonly DateTimeOffset utcNow = new(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));

    public override DateTimeOffset GetUtcNow() => utcNow;
}
