using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Spectre.Console;
using Spectre.Console.Json;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Chat client that logs messages and responses to the console in JSON format using Spectre.Console.
/// </summary>
/// <param name="innerClient"></param>
public class JsonConsoleLoggingChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
{
    /// <summary>
    /// Whether to include additional properties in the JSON output.
    /// </summary>
    public bool IncludeAdditionalProperties { get; set; } = true;

    /// <summary>
    /// Optional maximum length to render for string values. Replaces remaining characters with "...".
    /// </summary>
    public int? MaxLength { get; set; }

    public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        AnsiConsole.Write(new Panel(new JsonText(new
        {
            messages = messages.Where(x => x.Role != ChatRole.System).ToArray(),
            options
        }.ToJsonString(MaxLength, IncludeAdditionalProperties))));

        var response = await InnerClient.GetResponseAsync(messages, options, cancellationToken);

        AnsiConsole.Write(new Panel(new JsonText(response.ToJsonString(MaxLength, IncludeAdditionalProperties))));
        return response;
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        AnsiConsole.Write(new Panel(new JsonText(new
        {
            messages = messages.Where(x => x.Role != ChatRole.System).ToArray(),
            options
        }.ToJsonString(MaxLength, IncludeAdditionalProperties))));

        List<ChatResponseUpdate> updates = [];

        await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
        {
            updates.Add(update);
            yield return update;
        }

        AnsiConsole.Write(new Panel(new JsonText(updates.ToJsonString(MaxLength, IncludeAdditionalProperties))));
    }
}

