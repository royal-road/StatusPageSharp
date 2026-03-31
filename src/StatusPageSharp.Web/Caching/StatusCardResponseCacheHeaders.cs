using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace StatusPageSharp.Web.Caching;

public static class StatusCardResponseCacheHeaders
{
    private const string CdnCacheControlHeaderName = "CDN-Cache-Control";
    private const string CloudflareCdnCacheControlHeaderName = "Cloudflare-CDN-Cache-Control";
    private const string CacheControlValue = "public, max-age=30, s-maxage=30";

    public static void Apply(IHeaderDictionary headers)
    {
        headers[HeaderNames.CacheControl] = CacheControlValue;
        headers[CdnCacheControlHeaderName] = CacheControlValue;
        headers[CloudflareCdnCacheControlHeaderName] = CacheControlValue;
    }
}
