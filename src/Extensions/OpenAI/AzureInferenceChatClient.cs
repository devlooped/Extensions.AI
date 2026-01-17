using System.Collections.Concurrent;
using Azure;
using Azure.AI.Inference;
using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI.OpenAI;

/// <summary>
/// An <see cref="IChatClient"/> implementation for Azure AI Inference that supports per-request model selection.
/// </summary>
class AzureInferenceChatClient : IChatClient
{
    readonly ConcurrentDictionary<string, IChatClient> clients = new();

    readonly string modelId;
    readonly ChatCompletionsClient client;
    readonly ChatClientMetadata? metadata;

    /// <summary>
    /// Initializes the client with the specified API key, model ID, and optional OpenAI client options.
    /// </summary>
    public AzureInferenceChatClient(Uri endpoint, AzureKeyCredential credential, string modelId, AzureAIInferenceClientOptions? options = default)
    {
        this.modelId = modelId;

        // NOTE: by caching the pipeline, we speed up creation of new chat clients per model, 
        // since the pipeline will be the same for all of them.
        client = new ChatCompletionsClient(endpoint, credential, options);
        metadata = client.AsIChatClient(modelId)
            .GetService(typeof(ChatClientMetadata)) as ChatClientMetadata;
    }

    /// <inheritdoc/>
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellation = default)
        => GetChatClient(options?.ModelId ?? modelId).GetResponseAsync(messages, options, cancellation);

    /// <inheritdoc/>
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellation = default)
        => GetChatClient(options?.ModelId ?? modelId).GetStreamingResponseAsync(messages, options, cancellation);

    IChatClient GetChatClient(string modelId) => clients.GetOrAdd(modelId, client.AsIChatClient);

    void IDisposable.Dispose() => GC.SuppressFinalize(this);

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType switch
    {
        Type t when t == typeof(ChatClientMetadata) => metadata,
        _ => null
    };
}
