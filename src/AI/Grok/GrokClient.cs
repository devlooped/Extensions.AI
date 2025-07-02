using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Devlooped.Extensions.AI;

public class GrokClient(string apiKey, OpenAIClientOptions options)
    : OpenAIClient(new ApiKeyCredential(apiKey), EnsureEndpoint(options))
{
    // This allows ChatOptions to request a different model than the one configured 
    // in the chat pipeline when GetChatClient(model).AsIChatClient() is called at registration time.
    readonly ConcurrentDictionary<string, GrokChatClientAdapter> adapters = new();
    readonly ConcurrentDictionary<string, IChatClient> clients = new();

    public GrokClient(string apiKey)
        : this(apiKey, new())
    {
    }

    IChatClient GetChatClientImpl(string model)
        // Gets the real chat client by prefixing so the overload invokes the base.
        => clients.GetOrAdd(model, key => GetChatClient("__" + model).AsIChatClient());

    /// <summary>
    /// Returns an adapter that surfaces an <see cref="IChatClient"/> interface that 
    /// can be used directly in the <see cref="ChatClientBuilder"/> pipeline builder.
    /// </summary>
    public override OpenAI.Chat.ChatClient GetChatClient(string model)
        // We need to differentiate getting a real chat client vs an adapter for pipeline setup.
        // The former is invoked by the adapter when it needs to invoke the actual chat client, 
        // which goes through the GetChatClientImpl. Since the method override is necessary to 
        // satisfy the usage pattern when configuring OpenAIClient with M.E.AI, we differentiate 
        // the internal call by adding a prefix we remove before calling downstream.
        => model.StartsWith("__") ? base.GetChatClient(model[2..]) : new GrokChatClientAdapter(this, model);

    static OpenAIClientOptions EnsureEndpoint(OpenAIClientOptions options)
    {
        if (options.Endpoint is null)
            options.Endpoint = new Uri("https://api.x.ai/v1");

        return options;
    }

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

    public class GrokChatClientAdapter(GrokClient client, string model) : OpenAI.Chat.ChatClient, IChatClient
    {
        void IDisposable.Dispose() { }

        object? IChatClient.GetService(Type serviceType, object? serviceKey) => client.GetChatClientImpl(model).GetService(serviceType, serviceKey);

        /// <summary>
        /// Routes the request to a client that matches the options' ModelId (if set), or 
        /// the default model when the adapter was created.
        /// </summary>
        Task<ChatResponse> IChatClient.GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellation)
            => client.GetChatClientImpl(options?.ModelId ?? model).GetResponseAsync(messages, SetOptions(options), cancellation);

        /// <summary>
        /// Routes the request to a client that matches the options' ModelId (if set), or 
        /// the default model when the adapter was created.
        /// </summary>
        IAsyncEnumerable<ChatResponseUpdate> IChatClient.GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellation)
            => client.GetChatClientImpl(options?.ModelId ?? model).GetStreamingResponseAsync(messages, SetOptions(options), cancellation);

        // These are the only two methods actually invoked by the AsIChatClient adapter from M.E.AI.OpenAI
        public override Task<ClientResult<OpenAI.Chat.ChatCompletion>> CompleteChatAsync(IEnumerable<OpenAI.Chat.ChatMessage>? messages, OpenAI.Chat.ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)} instead of invoking {nameof(OpenAIClientExtensions.AsIChatClient)} on this instance.");

        public override AsyncCollectionResult<OpenAI.Chat.StreamingChatCompletionUpdate> CompleteChatStreamingAsync(IEnumerable<OpenAI.Chat.ChatMessage>? messages, OpenAI.Chat.ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)} instead of invoking {nameof(OpenAIClientExtensions.AsIChatClient)} on this instance.");

        #region Unsupported

        public override ClientResult CompleteChat(BinaryContent? content, RequestOptions? options = null)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        public override ClientResult<OpenAI.Chat.ChatCompletion> CompleteChat(IEnumerable<OpenAI.Chat.ChatMessage>? messages, OpenAI.Chat.ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        public override ClientResult<OpenAI.Chat.ChatCompletion> CompleteChat(params OpenAI.Chat.ChatMessage[] messages)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        public override Task<ClientResult> CompleteChatAsync(BinaryContent? content, RequestOptions? options = null)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        public override Task<ClientResult<OpenAI.Chat.ChatCompletion>> CompleteChatAsync(params OpenAI.Chat.ChatMessage[] messages)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        public override CollectionResult<OpenAI.Chat.StreamingChatCompletionUpdate> CompleteChatStreaming(IEnumerable<OpenAI.Chat.ChatMessage>? messages, OpenAI.Chat.ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        public override CollectionResult<OpenAI.Chat.StreamingChatCompletionUpdate> CompleteChatStreaming(params OpenAI.Chat.ChatMessage[] messages)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        public override AsyncCollectionResult<OpenAI.Chat.StreamingChatCompletionUpdate> CompleteChatStreamingAsync(params OpenAI.Chat.ChatMessage[] messages)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        #endregion
    }
}

