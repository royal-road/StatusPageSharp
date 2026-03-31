using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using StatusPageSharp.Web.Caching;

namespace StatusPageSharp.Web.Tests.Caching;

public class StatusCardResponseCacheHeadersTests
{
    [Fact]
    public void Apply_SetsThirtySecondCacheHeaders_ForBrowsersAndCdns()
    {
        var headers = new HeaderDictionary();

        StatusCardResponseCacheHeaders.Apply(headers);

        Assert.Equal(
            "public, max-age=30, s-maxage=30",
            headers[HeaderNames.CacheControl].ToString()
        );
        Assert.Equal("public, max-age=30, s-maxage=30", headers["CDN-Cache-Control"].ToString());
        Assert.Equal(
            "public, max-age=30, s-maxage=30",
            headers["Cloudflare-CDN-Cache-Control"].ToString()
        );
    }
}
