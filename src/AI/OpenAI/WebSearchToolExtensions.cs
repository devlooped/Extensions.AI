using OpenAI.Responses;

namespace Devlooped.Extensions.AI.OpenAI;

public static class OpenAIWebSearchToolExtensions
{
    extension(WebSearchTool web)
    {
        /// <summary>
        /// Optional free text additional information about the region to be used in the search. 
        /// </summary>
        /// <see cref="https://platform.openai.com/docs/guides/tools-web-search?api-mode=responses#user-location"/>
        public string? Region
        {
            get => web.Properties.TryGetValue("Region", out var region) ? (string?)region : null;
            set
            {
                web.Properties["Region"] = value;
                web.Location = WebSearchUserLocation.CreateApproximateLocation(web.Country, value, web.City, web.TimeZone);
            }
        }

        /// <summary>
        /// Optional free text additional information about the city to be used in the search. 
        /// </summary>
        /// <see cref="https://platform.openai.com/docs/guides/tools-web-search?api-mode=responses#user-location"/>
        public string? City
        {
            get => web.Properties.TryGetValue("City", out var city) ? (string?)city : null;
            set
            {
                web.Properties["City"] = value;
                web.Location = WebSearchUserLocation.CreateApproximateLocation(web.Country, web.Region, value, web.TimeZone);
            }
        }

        /// <summary>
        /// Optional IANA timezone name to be used in the search.
        /// </summary>
        /// <see cref="https://platform.openai.com/docs/guides/tools-web-search?api-mode=responses#user-location"/>
        public string? TimeZone
        {
            get => web.Properties.TryGetValue("TimeZone", out var timeZone) ? (string?)timeZone : null;
            set
            {
                web.Properties["TimeZone"] = value;
                web.Location = WebSearchUserLocation.CreateApproximateLocation(web.Country, web.Region, web.City, value);
            }
        }

        /// <summary>
        /// Controls how much context is retrieved from the web to help the tool formulate a response. 
        /// </summary>
        public WebSearchContextSize? ContextSize
        {
            get => web.Properties.TryGetValue(nameof(WebSearchContextSize), out var size) && size is WebSearchContextSize contextSize
                ? contextSize : null;
            set
            {
                if (value.HasValue)
                    web.ContextSize = value.Value;
                else
                    web.Properties.Remove(nameof(WebSearchContextSize));
            }
        }
    }
}
