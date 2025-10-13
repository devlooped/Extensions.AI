using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Provides extension methods for <see cref="ClientPipelineOptions"/>.
/// </summary>
public static class ClientPipelineExtensions
{
    extension<TOptions>(TOptions) where TOptions : ClientPipelineOptions, new()
    {
        /// <summary>
        /// Creates an instance of the <see cref="TOptions"/> that can be observed for requests and responses.
        /// </summary>
        /// <param name="onRequest">A callback to process the <see cref="JsonNode"/> that was sent.</param>
        /// <param name="onResponse">A callback to process the <see cref="JsonNode"/> that was received.</param>
        /// <returns>A new instance of <typeparamref name="TOptions"/>.</returns>
        public static TOptions Observable(Action<JsonNode>? onRequest = default, Action<JsonNode>? onResponse = default)
            => new TOptions().Observe(onRequest, onResponse);
    }

    extension<TOptions>(TOptions options) where TOptions : ClientPipelineOptions
    {
#if NET9_0_OR_GREATER
        [System.Runtime.CompilerServices.OverloadResolutionPriority(100)]
#endif
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
        public TOptions Observe(Action<JsonNode>? onRequest = default, Action<JsonNode>? onResponse = default)
        {
            options.AddPolicy(new ObservePipelinePolicy(onRequest, onResponse), PipelinePosition.BeforeTransport);
            return options;
        }
    }

    class ObservePipelinePolicy(Action<JsonNode>? onRequest = default, Action<JsonNode>? onResponse = default) : PipelinePolicy
    {
        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            message.BufferResponse = true;
            NotifyRequest(message);
            ProcessNext(message, pipeline, currentIndex);
            NotifyResponse(message);
        }

        public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            message.BufferResponse = true;
            NotifyRequest(message);
            await ProcessNextAsync(message, pipeline, currentIndex);
            NotifyResponse(message);
        }

        void NotifyResponse(PipelineMessage message)
        {
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

        void NotifyRequest(PipelineMessage message)
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
        }
    }
}