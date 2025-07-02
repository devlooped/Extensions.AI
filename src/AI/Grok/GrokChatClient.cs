using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Devlooped.Extensions.AI;

/// <summary>
/// An <see cref="IChatClient"/> implementation for Grok.
/// </summary>
public class GrokChatClient : IChatClient
{
    readonly ConcurrentDictionary<string, IChatClient> clients = new();
    readonly string modelId;
    readonly ClientPipeline pipeline;
    readonly OpenAIClientOptions options;

    /// <summary>
    /// Initializes the client with the specified API key, model ID, and optional OpenAI client options.
    /// </summary>
    public GrokChatClient(string apiKey, string modelId, OpenAIClientOptions? options = default)
    {
        this.modelId = modelId;
        this.options = options ?? new();
        this.options.Endpoint ??= new Uri("https://api.x.ai/v1");

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

            if (options.Tools != null)
            {
                if (options.Tools.OfType<GrokSearchTool>().FirstOrDefault() is GrokSearchTool grokSearch)
                    search = grokSearch.Mode;
                else if (options.Tools.OfType<HostedWebSearchTool>().FirstOrDefault() is HostedWebSearchTool webSearch)
                    search = GrokSearch.Auto;

                // Grok doesn't support any other hosted search tools, so remove remaining ones
                // so they don't get copied over by the OpenAI client.
                //options.Tools = [.. options.Tools.Where(tool => tool is not HostedWebSearchTool)];
            }

            if (search != null)
                result.Search = search.Value;

            if (grok?.ReasoningEffort != null)
            {
                result.ReasoningEffortLevel = grok.ReasoningEffort switch
                {
                    ReasoningEffort.High => OpenAI.Chat.ChatReasoningEffortLevel.High,
                    // Grok does not support Medium, so we map it to Low too
                    _ => OpenAI.Chat.ChatReasoningEffortLevel.Low,
                };
            }

            return result;
        };

        return options;
    }

    void IDisposable.Dispose() { }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    // Allows creating the base OpenAIClient with a pre-created pipeline.
    class PipelineClient(ClientPipeline pipeline, OpenAIClientOptions options) : OpenAIClient(pipeline, options) { }

    class GrokCompletionOptions : OpenAI.Chat.ChatCompletionOptions
    {
        public GrokSearch Search { get; set; } = GrokSearch.Auto;

        protected override void JsonModelWriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions? options)
        {
            base.JsonModelWriteCore(writer, options);

            // "search_parameters": { "mode": "auto" } 
            writer.WritePropertyName("search_parameters");
            writer.WriteStartObject();
            writer.WriteString("mode", Search.ToString().ToLowerInvariant());
            writer.WriteEndObject();
        }
    }
}
