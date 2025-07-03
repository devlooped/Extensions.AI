using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Devlooped.Extensions.AI;
using Spectre.Console;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Adds console logging capabilities to the chat client and pipeline transport.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class JsonConsoleLoggingExtensions
{
    extension<TOptions>(TOptions pipelineOptions) where TOptions : ClientPipelineOptions
    {
        /// <summary>
        /// Observes the HTTP request and response messages from the underlying pipeline and renders them 
        /// to the console using Spectre.Console rich JSON formatting, but only if the console is interactive.
        /// </summary>
        /// <typeparam name="TOptions">The options type to configure for HTTP logging.</typeparam>
        /// <param name="pipelineOptions">The options instance to configure.</param>
        /// <see cref="ClientPipelineExtensions.Observe"/>
        public TOptions UseJsonConsoleLogging(JsonConsoleOptions? consoleOptions = null)
        {
            consoleOptions ??= JsonConsoleOptions.Default;

            if (consoleOptions.InteractiveConfirm && ConsoleExtensions.IsConsoleInteractive && !AnsiConsole.Confirm("Do you want to enable rich JSON console logging for HTTP pipeline messages?"))
                return pipelineOptions;

            if (consoleOptions.InteractiveOnly && !ConsoleExtensions.IsConsoleInteractive)
                return pipelineOptions;

            return pipelineOptions.Observe(
                request => AnsiConsole.Write(consoleOptions.CreatePanel(request)),
                response => AnsiConsole.Write(consoleOptions.CreatePanel(response)));
        }
    }

    extension(ChatClientBuilder builder)
    {
        /// <summary>
        /// Renders chat messages and responses to the console using Spectre.Console rich JSON formatting.
        /// </summary>
        /// <param name="builder">The builder in use.</param>
        /// <remarks>
        /// Confirmation will be asked if the console is interactive, otherwise, it will be 
        /// enabled unconditionally.
        /// </remarks>
        public ChatClientBuilder UseJsonConsoleLogging(JsonConsoleOptions? consoleOptions = null)
        {
            consoleOptions ??= JsonConsoleOptions.Default;

            if (consoleOptions.InteractiveConfirm && ConsoleExtensions.IsConsoleInteractive && !AnsiConsole.Confirm("Do you want to enable rich JSON console logging for HTTP pipeline messages?"))
                return builder;

            if (consoleOptions.InteractiveOnly && !ConsoleExtensions.IsConsoleInteractive)
                return builder;

            return builder.Use(inner => new JsonConsoleLoggingChatClient(inner, consoleOptions));
        }
    }

    class JsonConsoleLoggingChatClient(IChatClient inner, JsonConsoleOptions consoleOptions) : DelegatingChatClient(inner)
    {
        public override async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            AnsiConsole.Write(consoleOptions.CreatePanel(new
            {
                messages = messages.Where(x => x.Role != ChatRole.System).ToArray(),
                options
            }));

            var response = await InnerClient.GetResponseAsync(messages, options, cancellationToken);
            AnsiConsole.Write(consoleOptions.CreatePanel(response));

            return response;
        }

        public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            AnsiConsole.Write(consoleOptions.CreatePanel(new
            {
                messages = messages.Where(x => x.Role != ChatRole.System).ToArray(),
                options
            }));

            List<ChatResponseUpdate> updates = [];

            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                updates.Add(update);
                yield return update;
            }

            AnsiConsole.Write(consoleOptions.CreatePanel(updates));
        }
    }
}
