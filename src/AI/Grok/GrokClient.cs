using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Devlooped.Extensions.AI;

public class GrokClient(string apiKey, GrokClientOptions options)
    : OpenAIClient(new ApiKeyCredential(apiKey), options), IChatClient
{
    readonly GrokClientOptions clientOptions = options;
    readonly ConcurrentDictionary<string, IChatClient> clients = new();

    public GrokClient(string apiKey)
        : this(apiKey, new())
    {
    }

    void IDisposable.Dispose() { }
    object? IChatClient.GetService(Type serviceType, object? serviceKey) => default;

    Task<ChatResponse> IChatClient.GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellation)
        => GetClient(options).GetResponseAsync(messages, SetOptions(options), cancellation);

    IAsyncEnumerable<ChatResponseUpdate> IChatClient.GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellation)
        => GetClient(options).GetStreamingResponseAsync(messages, SetOptions(options), cancellation);

    IChatClient GetClient(ChatOptions? options) => clients.GetOrAdd(
        options?.ModelId ?? clientOptions.Model,
        model => base.GetChatClient(model).AsIChatClient());

    ChatOptions? SetOptions(ChatOptions? options)
    {
        if (options is null)
            return null;

        options.RawRepresentationFactory = _ =>
        {
            var result = new GrokCompletionOptions();
            var grok = options as GrokChatOptions;

            if (options.Tools != null)
            {
                if (options.Tools.OfType<GrokSearchTool>().FirstOrDefault() is GrokSearchTool grokSearch)
                    result.Search = grokSearch.Mode;
                else if (options.Tools.OfType<HostedWebSearchTool>().FirstOrDefault() is HostedWebSearchTool webSearch)
                    result.Search = GrokSearch.Auto;

                // Grok doesn't support any other hosted search tools, so remove remaining ones
                // so they don't get copied over by the OpenAI client.
                options.Tools = [.. options.Tools.Where(tool => tool is not HostedWebSearchTool)];
            }
            else if (grok is not null)
            {
                result.Search = grok.Search;
            }

            if (grok?.ReasoningEffort != null)
            {
                result.ReasoningEffortLevel = grok.ReasoningEffort switch
                {
                    ReasoningEffort.Low => OpenAI.Chat.ChatReasoningEffortLevel.Low,
                    ReasoningEffort.High => OpenAI.Chat.ChatReasoningEffortLevel.High,
                    _ => throw new ArgumentException($"Unsupported reasoning effort {grok.ReasoningEffort}")
                };
            }

            return result;
        };

        return options;
    }

    class SearchParameters
    {
        public GrokSearch Mode { get; set; } = GrokSearch.Auto;
    }

    class GrokCompletionOptions : OpenAI.Chat.ChatCompletionOptions
    {
        public GrokSearch Search { get; set; } = GrokSearch.Auto;

        protected override void JsonModelWriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions options)
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

