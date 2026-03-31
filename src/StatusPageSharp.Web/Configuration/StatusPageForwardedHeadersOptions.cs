namespace StatusPageSharp.Web.Configuration;

public sealed class StatusPageForwardedHeadersOptions
{
    public const string SectionName = "ForwardedHeaders";

    public bool AllowAnyProxy { get; init; }

    public string[] KnownIPNetworks { get; init; } = [];

    public string[] KnownProxies { get; init; } = [];
}
