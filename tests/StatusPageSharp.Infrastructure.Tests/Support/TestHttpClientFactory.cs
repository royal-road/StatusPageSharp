namespace StatusPageSharp.Infrastructure.Tests.Support;

public sealed class TestHttpClientFactory(HttpClient httpClient) : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => httpClient;
}
