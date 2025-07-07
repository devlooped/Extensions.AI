using System.ClientModel;
using System.ClientModel.Primitives;
using System.Collections.Concurrent;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;

namespace Devlooped.Extensions.AI.OpenAI;

/// <summary>
/// An <see cref="IChatClient"/> implementation for OpenAI.
/// </summary>
public class OpenAIChatClient : IChatClient
{
    readonly ConcurrentDictionary<string, IChatClient> clients = new();
    readonly string modelId;
    readonly ClientPipeline pipeline;
    readonly OpenAIClientOptions? options;

    /// <summary>
    /// Initializes the client with the specified API key, model ID, and optional OpenAI client options.
    /// </summary>
    public OpenAIChatClient(string apiKey, string modelId, OpenAIClientOptions? options = default)
    {
        this.modelId = modelId;
        this.options = options;

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
        => new PipelineClient(pipeline, options).GetOpenAIResponseClient(modelId).AsIChatClient());

    static ChatOptions? SetOptions(ChatOptions? options)
    {
        if (options is null)
            return null;

        if (options.ReasoningEffort is ReasoningEffort effort)
        {
            options.RawRepresentationFactory = _ => new ResponseCreationOptions
            {
                ReasoningOptions = new ResponseReasoningOptions(effort switch
                {
                    ReasoningEffort.High => ResponseReasoningEffortLevel.High,
                    ReasoningEffort.Medium => ResponseReasoningEffortLevel.Medium,
                    _ => ResponseReasoningEffortLevel.Low
                })
            };
        }

        return options;
    }

    void IDisposable.Dispose() { }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    // Allows creating the base OpenAIClient with a pre-created pipeline.
    class PipelineClient(ClientPipeline pipeline, OpenAIClientOptions? options) : OpenAIClient(pipeline, options) { }
}
