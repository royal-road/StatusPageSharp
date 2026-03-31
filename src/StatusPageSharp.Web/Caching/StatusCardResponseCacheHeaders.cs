using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace StatusPageSharp.Web.Caching;

public static class StatusCardResponseCacheHeaders
{
    private const string CdnCacheControlHeaderName = "CDN-Cache-Control";
    private const string CloudflareCdnCacheControlHeaderName = "Cloudflare-CDN-Cache-Control";
    private const string NoStoreDirective = "no-store";

    public static void Apply(IHeaderDictionary headers)
    {
        headers[HeaderNames.CacheControl] = NoStoreDirective;
        headers[CdnCacheControlHeaderName] = NoStoreDirective;
        headers[CloudflareCdnCacheControlHeaderName] = NoStoreDirective;
    }
}
