using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Devlooped.Extensions.AI.Grok;

/// <summary>
/// An <see cref="IChatClient"/> implementation for Grok.
/// </summary>
public partial class GrokChatClient : IChatClient
{
    readonly ConcurrentDictionary<string, IChatClient> clients = new();
    readonly string modelId;
    readonly ClientPipeline pipeline;
    readonly OpenAIClientOptions options;
    readonly ChatClientMetadata metadata;

    /// <summary>
    /// Initializes the client with the specified API key, model ID, and optional OpenAI client options.
    /// </summary>
    public GrokChatClient(string apiKey, string modelId, OpenAIClientOptions? options = default)
    {
        this.modelId = modelId;
        this.options = options ?? new();
        this.options.Endpoint ??= new Uri("https://api.x.ai/v1");
        metadata = new ChatClientMetadata("xai", this.options.Endpoint, modelId);

        // NOTE: by caching the pipeline, we speed up creation of new chat clients per model, 
        // since the pipeline will be the same for all of them.
        pipeline = new OpenAIClient(new ApiKeyCredential(apiKey), options).Pipeline;
    }

    /// <inheritdoc/>
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellation = default)
            => GetChatClient(options?.ModelId ?? modelId).GetResponseAsync(messages, SetOptions(options), cancellation);

    /// <inheritdoc/>
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellation = default)
            => GetChatClient(options?.ModelId ?? modelId).GetStreamingResponseAsync(messages, SetOptions(options), cancellation);

    IChatClient GetChatClient(string modelId) => clients.GetOrAdd(modelId, model
        => new PipelineClient(pipeline, options).GetChatClient(modelId).AsIChatClient());

    static ChatOptions? SetOptions(ChatOptions? options)
    {
        if (options is null)
            return null;

        options.RawRepresentationFactory = _ =>
        {
            var result = new GrokCompletionOptions();
            var grok = options as GrokChatOptions;
            var search = grok?.Search;
            var tool = options.Tools?.OfType<GrokSearchTool>().FirstOrDefault();
            GrokChatWebSearchOptions? searchOptions = default;

            if (search is not null && tool is null)
            {
                searchOptions = new GrokChatWebSearchOptions
                {
                    Mode = search.Value
                };
            }
            else if (tool is null && options.Tools?.OfType<WebSearchTool>().FirstOrDefault() is { } web)
            {
                searchOptions = new GrokChatWebSearchOptions
                {
                    Mode = GrokSearch.Auto,
                    Sources = [new GrokWebSource { Country = web.Country }]
                };
            }
            else if (tool is null && options.Tools?.OfType<HostedWebSearchTool>().FirstOrDefault() is not null)
            {
                searchOptions = new GrokChatWebSearchOptions
                {
                    Mode = GrokSearch.Auto
                };
            }
            else if (tool is not null)
            {
                searchOptions = new GrokChatWebSearchOptions
                {
                    Mode = tool.Mode,
                    FromDate = tool.FromDate,
                    ToDate = tool.ToDate,
                    MaxSearchResults = tool.MaxSearchResults,
                    Sources = tool.Sources
                };
            }

            if (searchOptions is not null)
            {
                result.WebSearchOptions = searchOptions;
            }

            if (grok?.ReasoningEffort != null)
            {
                result.ReasoningEffortLevel = grok.ReasoningEffort switch
                {
                    ReasoningEffort.High => global::OpenAI.Chat.ChatReasoningEffortLevel.High,
                    // Grok does not support Medium, so we map it to Low too
                    _ => global::OpenAI.Chat.ChatReasoningEffortLevel.Low,
                };
            }

            return result;
        };

        return options;
    }

    void IDisposable.Dispose() { }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType switch
    {
        Type t when t == typeof(ChatClientMetadata) => metadata,
        _ => null
    };

    // Allows creating the base OpenAIClient with a pre-created pipeline.
    class PipelineClient(ClientPipeline pipeline, OpenAIClientOptions options) : OpenAIClient(pipeline, options) { }

    class GrokChatWebSearchOptions : global::OpenAI.Chat.ChatWebSearchOptions
    {
        public GrokSearch Mode { get; set; } = GrokSearch.Auto;
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
        public int? MaxSearchResults { get; set; }
        public IList<GrokSource>? Sources { get; set; }
    }

    [JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower
#if DEBUG
        , WriteIndented = true
#endif
    )]
    [JsonSerializable(typeof(GrokChatWebSearchOptions))]
    [JsonSerializable(typeof(GrokSearch))]
    [JsonSerializable(typeof(GrokSource))]
    [JsonSerializable(typeof(GrokRssSource))]
    [JsonSerializable(typeof(GrokWebSource))]
    [JsonSerializable(typeof(GrokNewsSource))]
    [JsonSerializable(typeof(GrokXSource))]
    partial class GrokJsonContext : JsonSerializerContext
    {
        static readonly Lazy<JsonSerializerOptions> options = new(CreateDefaultOptions);

        /// <summary>
        /// Provides a pre-configured instance of <see cref="JsonSerializerOptions"/> that aligns with the context's settings.
        /// </summary>
        public static JsonSerializerOptions DefaultOptions { get => options.Value; }

        static JsonSerializerOptions CreateDefaultOptions()
        {
            JsonSerializerOptions options = new(Default.Options)
            {
                WriteIndented = Debugger.IsAttached,
                Converters =
                {
                    new JsonStringEnumConverter(new LowercaseNamingPolicy()),
                },
            };

            options.MakeReadOnly();
            return options;
        }

        class LowercaseNamingPolicy : JsonNamingPolicy
        {
            public override string ConvertName(string name) => name.ToLowerInvariant();
        }
    }

    class GrokCompletionOptions : global::OpenAI.Chat.ChatCompletionOptions
    {
        protected override void JsonModelWriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions? options)
        {
            var search = WebSearchOptions as GrokChatWebSearchOptions;
            // This avoids writing the default `web_search_options` property
            WebSearchOptions = null;

            base.JsonModelWriteCore(writer, options);

            if (search != null)
            {
                writer.WritePropertyName("search_parameters");
                JsonSerializer.Serialize(writer, search, GrokJsonContext.DefaultOptions);
            }
        }
    }
}
