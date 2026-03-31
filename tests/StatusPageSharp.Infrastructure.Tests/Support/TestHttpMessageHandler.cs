namespace StatusPageSharp.Infrastructure.Tests.Support;

public sealed class TestHttpMessageHandler(
    Func<HttpRequestMessage, HttpResponseMessage> responseFactory
) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    ) => Task.FromResult(responseFactory(request));
}
