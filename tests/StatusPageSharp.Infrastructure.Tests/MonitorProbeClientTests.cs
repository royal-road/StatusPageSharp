using System.Net.NetworkInformation;
using StatusPageSharp.Application.Models.Monitoring;
using StatusPageSharp.Domain.Enums;
using StatusPageSharp.Infrastructure.Monitoring;
using StatusPageSharp.Infrastructure.Tests.Support;

namespace StatusPageSharp.Infrastructure.Tests;

public class MonitorProbeClientTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsSuccess_WhenIcmpProbeSucceeds()
    {
        var client = CreateClient(
            new TestPingClient(
                async (_, _, cancellationToken) =>
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(15), cancellationToken);
                    return IPStatus.Success;
                }
            )
        );

        var result = await client.ExecuteAsync(CreateIcmpRequest(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CheckFailureKind.None, result.FailureKind);
        Assert.Null(result.FailureMessage);
        Assert.True(result.DurationMilliseconds >= 10);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsTimeout_WhenIcmpProbeTimesOut()
    {
        var client = CreateClient(
            new TestPingClient((_, _, _) => Task.FromResult(IPStatus.TimedOut))
        );

        var result = await client.ExecuteAsync(CreateIcmpRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckFailureKind.Timeout, result.FailureKind);
        Assert.Equal("The ICMP probe timed out.", result.FailureMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsIcmpFailure_WhenIcmpProbeReturnsNonSuccessStatus()
    {
        var client = CreateClient(
            new TestPingClient((_, _, _) => Task.FromResult(IPStatus.DestinationHostUnreachable))
        );

        var result = await client.ExecuteAsync(CreateIcmpRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckFailureKind.IcmpFailure, result.FailureKind);
        Assert.Equal("Received ICMP status DestinationHostUnreachable.", result.FailureMessage);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsActionableError_WhenIcmpSupportIsUnavailable()
    {
        var client = CreateClient(
            new TestPingClient(
                (_, _, _) => throw new PlatformNotSupportedException("ICMP is not available.")
            )
        );

        var result = await client.ExecuteAsync(CreateIcmpRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckFailureKind.Exception, result.FailureKind);
        Assert.Equal(
            "ICMP is not available. ICMP probing requires either raw socket access or the system ping utility in the worker image.",
            result.FailureMessage
        );
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsException_WhenPingClientThrowsUnexpectedError()
    {
        var client = CreateClient(
            new TestPingClient(
                (_, _, _) => throw new InvalidOperationException("Unexpected ICMP failure.")
            )
        );

        var result = await client.ExecuteAsync(CreateIcmpRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(CheckFailureKind.Exception, result.FailureKind);
        Assert.Equal("Unexpected ICMP failure.", result.FailureMessage);
    }

    private static MonitorProbeClient CreateClient(IPingClient pingClient) =>
        new(new TestHttpClientFactory(new HttpClient()), pingClient);

    private static CheckProbeRequest CreateIcmpRequest() =>
        new(
            Guid.NewGuid(),
            MonitorType.Icmp,
            "status.example.com",
            null,
            null,
            "GET",
            null,
            null,
            "200-299",
            null,
            true,
            5
        );
}
