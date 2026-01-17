using Microsoft.Extensions.AI;
using OpenAI.Responses;
using WebSearch = OpenAI.Responses.WebSearchTool;

namespace Devlooped.Extensions.AI.OpenAI;

/// <summary>
/// Basic web search tool that can limit the search to a specific country.
/// </summary>
public class WebSearchTool : HostedWebSearchTool
{
    readonly Dictionary<string, object?> additionalProperties = new();
    string? country;
    string? region;
    string? city;
    string? timeZone;
    string[]? allowedDomains;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSearchTool"/> class with the specified country.
    /// </summary>
    /// <param name="country">ISO alpha-2 country code.</param>
    public WebSearchTool(string? country = null) => Country = country;

    /// <summary>
    /// Sets the user's country for web search results, using the ISO alpha-2 code.
    /// </summary>
    public string? Country
    {
        get => country;
        set
        {
            country = value;
            UpdateUserLocation();
        }
    }

    /// <summary>
    /// Optional free text additional information about the region to be used in the search.
    /// </summary>
    public string? Region
    {
        get => region;
        set
        {
            region = value;
            UpdateUserLocation();
        }
    }

    /// <summary>
    /// Optional free text additional information about the city to be used in the search.
    /// </summary>
    public string? City
    {
        get => city;
        set
        {
            city = value;
            UpdateUserLocation();
        }
    }

    /// <summary>
    /// Optional IANA timezone name to be used in the search.
    /// </summary>
    public string? TimeZone
    {
        get => timeZone;
        set
        {
            timeZone = value;
            UpdateUserLocation();
        }
    }

    /// <summary>
    /// Optional list of allowed domains to restrict the web search.
    /// </summary>
    public string[]? AllowedDomains
    {
        get => allowedDomains;
        set
        {
            allowedDomains = value;
            if (value is { Length: > 0 })
                additionalProperties[nameof(WebSearch.Filters)] = new WebSearchToolFilters { AllowedDomains = value };
            else
                additionalProperties.Remove(nameof(WebSearch.Filters));
        }
    }

    /// <inheritdoc/>
    public override IReadOnlyDictionary<string, object?> AdditionalProperties => additionalProperties;

    void UpdateUserLocation()
    {
        if (country != null || region != null || city != null || timeZone != null)
            additionalProperties[nameof(WebSearch.UserLocation)] =
                WebSearchToolLocation.CreateApproximateLocation(country, region, city, timeZone);
        else
            additionalProperties.Remove(nameof(WebSearch.UserLocation));
    }
}
