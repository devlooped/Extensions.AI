using System.ComponentModel;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI.Grok;

/// <summary>
/// Enables or disables Grok's search tool.
/// See https://docs.x.ai/docs/guides/tools/search-tools
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("No longer supported in agent search too. Use either GrokSearchTool or GrokXSearchTool")]
public enum GrokSearch
{
    /// <summary>
    /// (default): Search tool is available to the model, but the model automatically decides whether to perform search.
    /// </summary>
    Auto,
    /// <summary>
    /// Enables search tool.
    /// </summary>
    On,
    /// <summary>
    /// Disables search tool and uses the model without accessing additional information from data sources.
    /// </summary>
    Off
}

/// <summary>
/// Configures Grok's agentic search tool. 
/// See https://docs.x.ai/docs/guides/tools/search-tools.
/// </summary>
public class GrokSearchTool : HostedWebSearchTool
{
    public GrokSearchTool() { }

    /// <inheritdoc/>
    public override string Name => "Search";
    /// <inheritdoc/>
    public override string Description => "Performs search using X.AI";

    /// <summary>Use to make the web search only perform the search and web browsing on web pages that fall within the specified domains. Can include a maximum of five domains.</summary>
    public IList<string>? AllowedDomains { get; set; }
    /// <summary>Use to prevent the model from including the specified domains in any web search tool invocations and from browsing any pages on those domains. Can include a maximum of five domains.</summary>
    public IList<string>? ExcludedDomains { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/tools/search-tools#enable-image-understanding</summary>
    public bool EnableImageUnderstanding { get; set; }

    #region Legacy Live Search
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Legacy live search mode is not available in new agentic search tool.")]
    public GrokSearchTool(GrokSearch mode) => Mode = mode;
    /// <summary>Sets the search mode for Grok's live search capabilities.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Legacy live search mode is not available in new agentic search tool.")]
    public GrokSearch Mode { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/live-search#set-date-range-of-the-search-data</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Date range can only be applied to X source.")]
    public DateOnly? FromDate { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/live-search#set-date-range-of-the-search-data</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Date range can only be applied to X search tool.")]
    public DateOnly? ToDate { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/live-search#limit-the-maximum-amount-of-data-sources</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("No longer supported in search tool.")]
    public int? MaxSearchResults { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/live-search#data-sources-and-parameters</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("No longer supported in search tool.")]
    public IList<GrokSource>? Sources { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/live-search#returning-citations</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("No longer supported in search tool.")]
    public bool? ReturnCitations { get; set; }
    #endregion
}

/// <summary>
/// Configures Grok's agentic search tool for X. 
/// See https://docs.x.ai/docs/guides/tools/search-tools.
/// </summary>
public class GrokXSearchTool : HostedWebSearchTool
{
    /// <summary>See https://docs.x.ai/docs/guides/tools/search-tools#only-consider-x-posts-from-specific-handles</summary>
    [JsonPropertyName("allowed_x_handles")]
    public IList<string>? AllowedHandles { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/tools/search-tools#exclude-x-posts-from-specific-handles</summary>
    [JsonPropertyName("excluded_x_handles")]
    public IList<string>? ExcludedHandles { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/tools/search-tools#date-range</summary>
    public DateOnly? FromDate { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/tools/search-tools#date-range</summary>
    public DateOnly? ToDate { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/tools/search-tools#enable-image-understanding-1</summary>
    public bool EnableImageUnderstanding { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/tools/search-tools#enable-video-understanding</summary>
    public bool EnableVideoUnderstanding { get; set; }
}

/// <summary>Grok Live Search data source base type.</summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(GrokWebSource), "web")]
[JsonDerivedType(typeof(GrokNewsSource), "news")]
[JsonDerivedType(typeof(GrokRssSource), "rss")]
[JsonDerivedType(typeof(GrokXSource), "x")]
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("No longer supported in agent search too. Use either GrokSearchTool or GrokXSearchTool")]
public abstract class GrokSource { }

/// <summary>Search-based data source base class providing common properties for `web` and `news` sources.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("No longer supported in agent search too. Use either GrokSearchTool or GrokXSearchTool")]
public abstract class GrokSearchSource : GrokSource
{
    /// <summary>Include data from a specific country/region by specifying the ISO alpha-2 code of the country.</summary>
    public string? Country { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/live-search#parameter-safe_search-supported-by-web-and-news</summary>
    public bool? SafeSearch { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/live-search#parameter-excluded_websites-supported-by-web-and-news</summary>
    public IList<string>? ExcludedWebsites { get; set; }
}

/// <summary>Web live search source.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("No longer supported in agent search too. Use either GrokSearchTool or GrokXSearchTool")]
public class GrokWebSource : GrokSearchSource
{
    /// <summary>See https://docs.x.ai/docs/guides/live-search#parameter-allowed_websites-supported-by-web</summary>
    public IList<string>? AllowedWebsites { get; set; }
}

/// <summary>News live search source.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("No longer supported in agent search too. Use either GrokSearchTool or GrokXSearchTool")]
public class GrokNewsSource : GrokSearchSource { }

/// <summary>RSS live search source.</summary>
/// <param name="rss">The RSS feed to search.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("No longer supported in agent search too. Use either GrokSearchTool or GrokXSearchTool")]
public class GrokRssSource(string rss) : GrokSource
{
    /// <summary>See https://docs.x.ai/docs/guides/live-search#parameter-link-supported-by-rss</summary>
    public IList<string>? Links { get; set; } = [rss];
}

/// <summary>X live search source./summary>
[EditorBrowsable(EditorBrowsableState.Never)]
[Obsolete("No longer supported in agent search too. Use GrokXSearchTool")]
public class GrokXSource : GrokSearchSource
{
    /// <summary>See https://docs.x.ai/docs/guides/live-search#parameter-excluded_x_handles-supported-by-x</summary>
    [JsonPropertyName("excluded_x_handles")]
    public IList<string>? ExcludedHandles { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/live-search#parameter-included_x_handles-supported-by-x</summary>
    [JsonPropertyName("included_x_handles")]
    public IList<string>? IncludedHandles { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/live-search#parameters-post_favorite_count-and-post_view_count-supported-by-x</summary>
    [JsonPropertyName("post_favorite_count")]
    public int? FavoriteCount { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/live-search#parameters-post_favorite_count-and-post_view_count-supported-by-x</summary>
    [JsonPropertyName("post_view_count")]
    public int? ViewCount { get; set; }
    [JsonPropertyName("from_date")]
    public DateOnly? FromDate { get; set; }
    /// <summary>See https://docs.x.ai/docs/guides/live-search#set-date-range-of-the-search-data</summary>
    [JsonPropertyName("to_date")]
    public DateOnly? ToDate { get; set; }
}