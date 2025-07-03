using System.ClientModel.Primitives;
using System.Text.Json.Nodes;
using OpenAI;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Shortcut factory methods for creating <see cref="ClientPipelineOptions"/> like 
/// <see cref="OpenAIClientOptions"/> that provide convenient initialization options.
/// </summary>
public static class ClientOptions
{
    /// <summary>
    /// Creates an obserbable <see cref="OpenAIClientOptions"/> instance that can 
    /// be used to log requests and responses.
    /// </summary>
    /// <param name="onRequest">A callback to process the <see cref="JsonNode"/> that was sent.</param>
    /// <param name="onResponse">A callback to process the <see cref="JsonNode"/> that was received.</param>
    public static OpenAIClientOptions Observable(Action<JsonNode>? onRequest = default, Action<JsonNode>? onResponse = default)
        => Observable<OpenAIClientOptions>(onRequest, onResponse);

    /// <summary>
    /// Creates an obserbable <see cref="ClientPipelineOptions"/>-derived instance 
    /// that can be used to log requests and responses.
    /// </summary>
    /// <param name="onRequest">A callback to process the <see cref="JsonNode"/> that was sent.</param>
    /// <param name="onResponse">A callback to process the <see cref="JsonNode"/> that was received.</param>
    public static TOptions Observable<TOptions>(Action<JsonNode>? onRequest = default, Action<JsonNode>? onResponse = default)
        where TOptions : ClientPipelineOptions, new()
        => new TOptions().Observe(onRequest, onResponse);
}
