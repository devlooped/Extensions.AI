using System.ComponentModel;
using System.Runtime.CompilerServices;
using Devlooped.Extensions.AI;
using Spectre.Console;
using Spectre.Console.Json;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Adds console logging capabilities to the chat client.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class JsonConsoleLoggingExtensions
{
    /// <summary>
    /// Renders chat messages and responses to the console using Spectre.Console rich JSON formatting.
    /// </summary>
    /// <param name="builder">The builder in use.</param>
    /// <param name="askConfirmation">If true, prompts the user for confirmation before enabling console logging.</param>
    public static ChatClientBuilder UseJsonConsoleLogging(this ChatClientBuilder builder, bool askConfirmation = false)
    {
        if (askConfirmation && !AnsiConsole.Confirm("Do you want to enable console logging for chat messages?"))
            return builder;

        return builder.Use(inner => new ConsoleLoggingChatClient(inner));
    }

    class ConsoleLoggingChatClient(IChatClient innerClient) : DelegatingChatClient(innerClient)
    {
        public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            AnsiConsole.Write(new Panel(new JsonText(new
            {
                messages = messages.Where(x => x.Role != ChatRole.System).ToArray(),
                options
            }.ToJsonString())));

            var response = await InnerClient.GetResponseAsync(messages, options, cancellationToken);

            AnsiConsole.Write(new Panel(new JsonText(response.ToJsonString())));
            return response;
        }

        public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            AnsiConsole.Write(new Panel(new JsonText(new
            {
                messages = messages.Where(x => x.Role != ChatRole.System).ToArray(),
                options
            }.ToJsonString())));

            List<ChatResponseUpdate> updates = [];

            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                updates.Add(update);
                yield return update;
            }

            AnsiConsole.Write(new Panel(new JsonText(updates.ToJsonString())));
        }
    }
}
