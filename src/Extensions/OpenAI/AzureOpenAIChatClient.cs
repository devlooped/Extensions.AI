using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI.OpenAI;

/// <summary>
/// An <see cref="IChatClient"/> implementation for Azure OpenAI that supports per-request model selection.
/// </summary>
internal class AzureOpenAIChatClient : IChatClient
{
    readonly ConcurrentDictionary<string, IChatClient> clients = new();

    readonly Uri endpoint;
    readonly string modelId;
    readonly ClientPipeline pipeline;
    readonly AzureOpenAIClientOptions options;
    readonly ChatClientMetadata? metadata;

    /// <summary>
    /// Initializes the client with the given endpoint, API key, model ID, and optional Azure OpenAI client options.
    /// </summary>
    public AzureOpenAIChatClient(Uri endpoint, ApiKeyCredential credential, string modelId, AzureOpenAIClientOptions? options = default)
    {
        this.endpoint = endpoint;
        this.modelId = modelId;
        this.options = options ?? new();

        // NOTE: by caching the pipeline, we speed up creation of new chat clients per model, 
        // since the pipeline will be the same for all of them.
        var client = new AzureOpenAIClient(endpoint, credential, options);
        metadata = client.GetChatClient(modelId)
            .AsIChatClient()
            .GetService(typeof(ChatClientMetadata)) as ChatClientMetadata;

        metadata = new ChatClientMetadata(
            providerName: "azure.ai.openai",
            providerUri: metadata?.ProviderUri ?? endpoint,
            defaultModelId: metadata?.DefaultModelId ?? modelId);

        pipeline = client.Pipeline;
    }

    /// <inheritdoc/>
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellation = default)
        => GetChatClient(options?.ModelId ?? modelId).GetResponseAsync(messages, options, cancellation);

    /// <inheritdoc/>
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellation = default)
        => GetChatClient(options?.ModelId ?? modelId).GetStreamingResponseAsync(messages, options, cancellation);

    IChatClient GetChatClient(string modelId) => clients.GetOrAdd(modelId, model
        => new PipelineClient(pipeline, endpoint, options).GetOpenAIResponseClient(modelId).AsIChatClient());

    void IDisposable.Dispose() => GC.SuppressFinalize(this);

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType switch
    {
        Type t when t == typeof(ChatClientMetadata) => metadata,
        _ => null
    };

    // Allows creating the base OpenAIClient with a pre-created pipeline.
    class PipelineClient(ClientPipeline pipeline, Uri endpoint, AzureOpenAIClientOptions options) : AzureOpenAIClient(pipeline, endpoint, options) { }
}
