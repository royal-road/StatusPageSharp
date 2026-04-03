using System.Net.NetworkInformation;

namespace StatusPageSharp.Infrastructure.Monitoring;

public class SystemPingClient : IPingClient
{
    public async Task<IPStatus> SendAsync(
        string host,
        TimeSpan timeout,
        CancellationToken cancellationToken
    )
    {
        using var ping = new Ping();
        var reply = await ping.SendPingAsync(
            host,
            timeout,
            buffer: null,
            options: null,
            cancellationToken
        );
        return reply.Status;
    }
}
