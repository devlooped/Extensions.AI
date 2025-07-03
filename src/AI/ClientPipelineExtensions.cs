using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Devlooped.Extensions.AI;

public static class ClientPipelineExtensions
{
    /// <summary>
    /// Adds a <see cref="PipelinePolicy"/> that observes requests and response 
    /// messages from the <see cref="ClientPipeline"/> and notifies the provided 
    /// callbacks with the JSON representation of the HTTP messages.
    /// </summary>
    /// <typeparam name="TOptions">The options type to configure for HTTP logging.</typeparam>
    /// <param name="pipelineOptions">The options instance to configure.</param>
    /// <param name="onRequest">A callback to process the <see cref="JsonNode"/> that was sent.</param>
    /// <param name="onResponse">A callback to process the <see cref="JsonNode"/> that was received.</param>
    /// <remarks>
    /// This is the lowst-level logging after all chat pipeline processing has been done.
    /// If no <see cref="JsonNode"/> can be parsed from the request or response, 
    /// the callbacks will not be invoked.
    /// </remarks>
    public static TOptions Observe<TOptions>(this TOptions pipelineOptions,
        Action<JsonNode>? onRequest = default, Action<JsonNode>? onResponse = default)
        where TOptions : ClientPipelineOptions
    {
        pipelineOptions.AddPolicy(new ObservePipelinePolicy(onRequest, onResponse), PipelinePosition.BeforeTransport);
        return pipelineOptions;
    }

    class ObservePipelinePolicy(Action<JsonNode>? onRequest = default, Action<JsonNode>? onResponse = default) : PipelinePolicy
    {
        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            message.BufferResponse = true;
            ProcessNext(message, pipeline, currentIndex);
            NotifyObservers(message);
        }

        public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            message.BufferResponse = true;
            await ProcessNextAsync(message, pipeline, currentIndex);
            NotifyObservers(message);
        }

        void NotifyObservers(PipelineMessage message)
        {
            if (onRequest != null && message.Request.Content != null)
            {
                using var memory = new MemoryStream();
                message.Request.Content.WriteTo(memory);
                memory.Position = 0;
                using var reader = new StreamReader(memory);
                var content = reader.ReadToEnd();
                try
                {
                    if (JsonNode.Parse(content) is { } node)
                        onRequest.Invoke(node!);
                }
                catch (JsonException)
                {
                    // We ignore invalid JSON
                }
            }

            if (onResponse != null && message.Response != null)
            {
                try
                {
                    if (JsonNode.Parse(message.Response.Content.ToString()) is { } node)
                        onResponse.Invoke(node!);
                }
                catch (JsonException)
                {
                    // We ignore invalid JSON
                }
            }
        }
    }
}
