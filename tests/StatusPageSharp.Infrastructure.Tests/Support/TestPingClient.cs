using System.Net.NetworkInformation;
using StatusPageSharp.Infrastructure.Monitoring;

namespace StatusPageSharp.Infrastructure.Tests.Support;

public class TestPingClient(
    Func<string, TimeSpan, CancellationToken, Task<IPStatus>>? implementation = null
) : IPingClient
{
    public Task<IPStatus> SendAsync(
        string host,
        TimeSpan timeout,
        CancellationToken cancellationToken
    ) => (implementation ?? DefaultImplementation)(host, timeout, cancellationToken);

    private static Task<IPStatus> DefaultImplementation(
        string host,
        TimeSpan timeout,
        CancellationToken cancellationToken
    ) => Task.FromResult(IPStatus.Success);
}
