using System.Net.NetworkInformation;

namespace StatusPageSharp.Infrastructure.Monitoring;

public interface IPingClient
{
    Task<IPStatus> SendAsync(string host, TimeSpan timeout, CancellationToken cancellationToken);
}
