using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Devlooped.Extensions.AI.Grok;

/// <summary>
/// Provides an OpenAI compability client for Grok. It's recommended you 
/// use <see cref="GrokChatClient2"/> directly for chat-only scenarios.
/// </summary>
public class GrokClient2(string apiKey, OpenAIClientOptions? options = null)
    : OpenAIClient(new ApiKeyCredential(apiKey), EnsureEndpoint(options))
{
    readonly ConcurrentDictionary<string, IChatClient> clients = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="GrokClient2"/> with the specified API key.
    /// </summary>
    public GrokClient2(string apiKey) : this(apiKey, new()) { }

    IChatClient GetChatClientImpl(string model) => clients.GetOrAdd(model, key => new GrokChatClient2(apiKey, key, options));

    /// <summary>
    /// Returns an adapter that surfaces an <see cref="IChatClient"/> interface that 
    /// can be used directly in the <see cref="ChatClientBuilder"/> pipeline builder.
    /// </summary>
    public override global::OpenAI.Chat.ChatClient GetChatClient(string model) => new GrokChatClientAdapter(this, model);

    static OpenAIClientOptions EnsureEndpoint(OpenAIClientOptions? options)
    {
        options ??= new();
        options.Endpoint ??= new Uri("https://api.x.ai/v1");
        return options;
    }

    // This adapter is provided for compatibility with the documented usage for 
    // OpenAI in MEAI docs. Most typical case would be to just create an <see cref="GrokChatClient"/> directly.
    // This throws on any non-IChatClient invoked methods in the AsIChatClient adapter, and 
    // forwards the IChatClient methods to the GrokChatClient implementation which is cached per client.
    class GrokChatClientAdapter(GrokClient2 client, string model) : global::OpenAI.Chat.ChatClient, IChatClient
    {
        void IDisposable.Dispose() { }

        object? IChatClient.GetService(Type serviceType, object? serviceKey) => client.GetChatClientImpl(model).GetService(serviceType, serviceKey);

        /// <summary>
        /// Routes the request to a client that matches the options' ModelId (if set), or 
        /// the default model when the adapter was created.
        /// </summary>
        Task<ChatResponse> IChatClient.GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellation)
            => client.GetChatClientImpl(options?.ModelId ?? model).GetResponseAsync(messages, options, cancellation);

        /// <summary>
        /// Routes the request to a client that matches the options' ModelId (if set), or 
        /// the default model when the adapter was created.
        /// </summary>
        IAsyncEnumerable<ChatResponseUpdate> IChatClient.GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options, CancellationToken cancellation)
            => client.GetChatClientImpl(options?.ModelId ?? model).GetStreamingResponseAsync(messages, options, cancellation);

        // These are the only two methods actually invoked by the AsIChatClient adapter from M.E.AI.OpenAI
        public override Task<ClientResult<global::OpenAI.Chat.ChatCompletion>> CompleteChatAsync(IEnumerable<global::OpenAI.Chat.ChatMessage>? messages, global::OpenAI.Chat.ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)} instead of invoking {nameof(OpenAIClientExtensions.AsIChatClient)} on this instance.");

        public override AsyncCollectionResult<global::OpenAI.Chat.StreamingChatCompletionUpdate> CompleteChatStreamingAsync(IEnumerable<global::OpenAI.Chat.ChatMessage>? messages, global::OpenAI.Chat.ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)} instead of invoking {nameof(OpenAIClientExtensions.AsIChatClient)} on this instance.");

        #region Unsupported

        public override ClientResult CompleteChat(BinaryContent? content, RequestOptions? options = null)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        public override ClientResult<global::OpenAI.Chat.ChatCompletion> CompleteChat(IEnumerable<global::OpenAI.Chat.ChatMessage>? messages, global::OpenAI.Chat.ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        public override ClientResult<global::OpenAI.Chat.ChatCompletion> CompleteChat(params global::OpenAI.Chat.ChatMessage[] messages)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        public override Task<ClientResult> CompleteChatAsync(BinaryContent? content, RequestOptions? options = null)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        public override Task<ClientResult<global::OpenAI.Chat.ChatCompletion>> CompleteChatAsync(params global::OpenAI.Chat.ChatMessage[] messages)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        public override CollectionResult<global::OpenAI.Chat.StreamingChatCompletionUpdate> CompleteChatStreaming(IEnumerable<global::OpenAI.Chat.ChatMessage>? messages, global::OpenAI.Chat.ChatCompletionOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        public override CollectionResult<global::OpenAI.Chat.StreamingChatCompletionUpdate> CompleteChatStreaming(params global::OpenAI.Chat.ChatMessage[] messages)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        public override AsyncCollectionResult<global::OpenAI.Chat.StreamingChatCompletionUpdate> CompleteChatStreamingAsync(params global::OpenAI.Chat.ChatMessage[] messages)
            => throw new NotSupportedException($"Consume directly as an {nameof(IChatClient)}.");

        #endregion
    }
}

