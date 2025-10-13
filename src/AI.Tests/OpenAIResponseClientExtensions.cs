using System.ClientModel;
using OpenAI.Responses;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Provides extension methods for working with OpenAI.Responses.OpenAIResponseClient.
/// </summary>
public static class OpenAIResponseClientExtensions
{
    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="OpenAIResponseClient"/> that supports 
    /// additional OpenAI-specific tools.</summary>
    /// <param name="responseClient">The client.</param>
    /// <param name="tools">Additional tools to configure in the client before invocation.</param>
    /// <returns>An <see cref="IChatClient"/> that can be used to converse via the <see cref="OpenAIResponseClient"/>.</returns>
    public static IChatClient AsIChatClient(this OpenAIResponseClient responseClient, params ResponseTool[] tools)
        => tools.Length == 0 ? OpenAIClientExtensions.AsIChatClient(responseClient) : new ToolsReponseClient(responseClient, tools).AsIChatClient();

    class ToolsReponseClient(OpenAIResponseClient inner, ResponseTool[] tools) : OpenAIResponseClient
    {
        public override Task<ClientResult<OpenAIResponse>> CreateResponseAsync(IEnumerable<ResponseItem> inputItems, ResponseCreationOptions? options = null, CancellationToken cancellationToken = default)
            => inner.CreateResponseAsync(inputItems, AddTools(options), cancellationToken);

        public override AsyncCollectionResult<StreamingResponseUpdate> CreateResponseStreamingAsync(IEnumerable<ResponseItem> inputItems, ResponseCreationOptions? options = null, CancellationToken cancellationToken = default)
            => inner.CreateResponseStreamingAsync(inputItems, AddTools(options), cancellationToken);

        ResponseCreationOptions? AddTools(ResponseCreationOptions? options)
        {
            if (options == null)
                return null;

            foreach (var tool in tools)
                options.Tools.Add(tool);

            return options;
        }
    }
}