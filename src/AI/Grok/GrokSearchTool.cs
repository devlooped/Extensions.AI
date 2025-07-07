using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Enables or disables Grok's live search capabilities.
/// See https://docs.x.ai/docs/guides/live-search#enabling-search
/// </summary>
public enum GrokSearch
{
    /// <summary>
    /// (default): Live search is available to the model, but the model automatically decides whether to perform live search.
    /// </summary>
    Auto,
    /// <summary>
    /// Enables live search.
    /// </summary>
    On,
    /// <summary>
    /// Disables search and uses the model without accessing additional information from data sources.
    /// </summary>
    Off
}

/// <summary>
/// Configures Grok's live search capabilities. 
/// See https://docs.x.ai/docs/guides/live-search.
/// </summary>
public class GrokSearchTool(GrokSearch mode) : HostedWebSearchTool
{
    /// <summary>
    /// Sets the search mode for Grok's live search capabilities.
    /// </summary>
    public GrokSearch Mode { get; } = mode;
    /// <inheritdoc/>
    public override string Name => "Live Search";
    /// <inheritdoc/>
    public override string Description => "Performs live search using X.AI";
    /// <summary>
    /// See https://docs.x.ai/docs/guides/live-search#set-date-range-of-the-search-data
    /// </summary>
    public DateOnly? FromDate { get; set; }
    /// <summary>
    /// See https://docs.x.ai/docs/guides/live-search#set-date-range-of-the-search-data
    /// </summary>
    public DateOnly? ToDate { get; set; }
    /// <summary>
    /// See https://docs.x.ai/docs/guides/live-search#limit-the-maximum-amount-of-data-sources
    /// </summary>
    public int? MaxSearchResults { get; set; }
    /// <summary>
    /// See https://docs.x.ai/docs/guides/live-search#data-sources-and-parameters
    /// </summary>
    public IList<GrokSource>? Sources { get; set; }
}

/// <summary>
/// Grok Live Search data source base type.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(GrokWebSource), "web")]
[JsonDerivedType(typeof(GrokNewsSource), "news")]
[JsonDerivedType(typeof(GrokRssSource), "rss")]
[JsonDerivedType(typeof(GrokXSource), "x")]
public abstract class GrokSource { }

/// <summary>
/// Search-based data source base class providing common properties for `web` and `news` sources.
/// </summary>
public abstract class GrokSearchSource : GrokSource
{
    /// <summary>
    /// Include data from a specific country/region by specifying the ISO alpha-2 code of the country.
    /// </summary>
    public string? Country { get; set; }
    /// <summary>
    /// See https://docs.x.ai/docs/guides/live-search#parameter-safe_search-supported-by-web-and-news
    /// </summary>
    public bool? SafeSearch { get; set; }
    /// <summary>
    /// See https://docs.x.ai/docs/guides/live-search#parameter-excluded_websites-supported-by-web-and-news
    /// </summary>
    public IList<string>? ExcludedWebsites { get; set; }
}

/// <summary>
/// Web live search source.
/// </summary>
public class GrokWebSource : GrokSearchSource
{
    /// <summary>
    /// See https://docs.x.ai/docs/guides/live-search#parameter-allowed_websites-supported-by-web
    /// </summary>
    public IList<string>? AllowedWebsites { get; set; }
}

/// <summary>
/// News live search source.
/// </summary>
public class GrokNewsSource : GrokSearchSource { }

/// <summary>
/// RSS live search source.
/// </summary>
/// <param name="rss">The RSS feed to search.</param>
public class GrokRssSource(string rss) : GrokSource
{
    /// <summary>
    /// See https://docs.x.ai/docs/guides/live-search#parameter-link-supported-by-rss
    /// </summary>
    public IList<string>? Links { get; set; } = [rss];
}

/// <summary>
/// X live search source.
/// </summary>
public class GrokXSource : GrokSearchSource
{
    /// <summary>
    /// See https://docs.x.ai/docs/guides/live-search#parameter-excluded_x_handles-supported-by-x
    /// </summary>
    [JsonPropertyName("excluded_x_handles")]
    public IList<string>? ExcludedHandles { get; set; }
    /// <summary>
    /// See https://docs.x.ai/docs/guides/live-search#parameter-included_x_handles-supported-by-x
    /// </summary>
    [JsonPropertyName("included_x_handles")]
    public IList<string>? IncludedHandles { get; set; }
    /// <summary>
    /// See https://docs.x.ai/docs/guides/live-search#parameters-post_favorite_count-and-post_view_count-supported-by-x
    /// </summary>
    [JsonPropertyName("post_favorite_count")]
    public int? FavoriteCount { get; set; }
    /// <summary>
    /// See https://docs.x.ai/docs/guides/live-search#parameters-post_favorite_count-and-post_view_count-supported-by-x
    /// </summary>
    [JsonPropertyName("post_view_count")]
    public int? ViewCount { get; set; }
}