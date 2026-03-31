using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using StatusPageSharp.Web.Caching;

namespace StatusPageSharp.Web.Tests.Caching;

public class StatusCardResponseCacheHeadersTests
{
    [Fact]
    public void Apply_SetsNoStoreHeaders_ForBrowsersAndCdns()
    {
        var headers = new HeaderDictionary();

        StatusCardResponseCacheHeaders.Apply(headers);

        Assert.Equal("no-store", headers[HeaderNames.CacheControl].ToString());
        Assert.Equal("no-store", headers["CDN-Cache-Control"].ToString());
        Assert.Equal("no-store", headers["Cloudflare-CDN-Cache-Control"].ToString());
    }
}
