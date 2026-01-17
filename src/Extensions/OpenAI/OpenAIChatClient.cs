using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using OpenAI;

namespace Devlooped.Extensions.AI.OpenAI;

/// <summary>
/// An <see cref="IChatClient"/> implementation for OpenAI that supports per-request model selection.
/// </summary>
internal class OpenAIChatClient : IChatClient
{
    readonly ConcurrentDictionary<string, IChatClient> clients = new();
    readonly string modelId;
    readonly ClientPipeline pipeline;
    readonly OpenAIClientOptions? options;
    readonly ChatClientMetadata? metadata;

    /// <summary>
    /// Initializes the client with the specified API key, model ID, and optional OpenAI client options.
    /// </summary>
    public OpenAIChatClient(string apiKey, string modelId, OpenAIClientOptions? options = default)
    {
        this.modelId = modelId;
        this.options = options;

        // NOTE: by caching the pipeline, we speed up creation of new chat clients per model, 
        // since the pipeline will be the same for all of them.
        var client = new OpenAIClient(new ApiKeyCredential(apiKey), options);
        metadata = client.GetChatClient(modelId)
            .AsIChatClient()
            .GetService(typeof(ChatClientMetadata)) as ChatClientMetadata;

        pipeline = client.Pipeline;
    }

    /// <inheritdoc/>
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellation = default)
            => GetChatClient(options?.ModelId ?? modelId).GetResponseAsync(messages, options, cancellation);

    /// <inheritdoc/>
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellation = default)
            => GetChatClient(options?.ModelId ?? modelId).GetStreamingResponseAsync(messages, options, cancellation);

    IChatClient GetChatClient(string modelId) => clients.GetOrAdd(modelId, model
        => new PipelineClient(pipeline, options).GetOpenAIResponseClient(modelId).AsIChatClient());

    void IDisposable.Dispose() => GC.SuppressFinalize(this);

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType switch
    {
        Type t when t == typeof(ChatClientMetadata) => metadata,
        _ => null
    };

    // Allows creating the base OpenAIClient with a pre-created pipeline.
    class PipelineClient(ClientPipeline pipeline, OpenAIClientOptions? options) : OpenAIClient(pipeline, options) { }
}
