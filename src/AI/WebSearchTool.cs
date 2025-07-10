using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Basic web search tool that can limit the search to a specific country.
/// </summary>
public class WebSearchTool : HostedWebSearchTool
{
    Dictionary<string, object?> additionalProperties;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSearchTool"/> class with the specified country.
    /// </summary>
    /// <param name="country">ISO alpha-2 country code.</param>
    public WebSearchTool(string country)
    {
        Country = country;
        additionalProperties = new Dictionary<string, object?>
        {
            { nameof(WebSearchUserLocation), WebSearchUserLocation.CreateApproximateLocation(country) }
        };
    }

    /// <summary>
    /// Sets the user's country for web search results, using the ISO alpha-2 code.
    /// </summary>
    public string Country { get; }

    internal WebSearchUserLocation Location
    {
        set => additionalProperties[nameof(WebSearchUserLocation)] = value;
    }

    internal WebSearchContextSize ContextSize
    {
        set => additionalProperties[nameof(WebSearchContextSize)] = value;
    }

    internal IDictionary<string, object?> Properties => additionalProperties;

    /// <inheritdoc/>
    public override IReadOnlyDictionary<string, object?> AdditionalProperties => additionalProperties;
}
