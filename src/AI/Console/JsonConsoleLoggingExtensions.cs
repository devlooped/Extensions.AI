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
    /// <summary>
    /// Sets a <see cref="ClientPipelineOptions.Transport"/> that renders HTTP messages to the 
    /// console using Spectre.Console rich JSON formatting, but only if the console is interactive.
    /// </summary>
    /// <typeparam name="TOptions">The options type to configure for HTTP logging.</typeparam>
    /// <param name="pipelineOptions">The options instance to configure.</param>
    /// <remarks>
    /// NOTE: this is the lowst-level logging after all chat pipeline processing has been done.
    /// <para>
    /// If the options already provide a transport, it will be wrapped with the console 
    /// logging transport to minimize the impact on existing configurations.
    /// </para>
    /// </remarks>
    public static TOptions UseJsonConsoleLogging<TOptions>(this TOptions pipelineOptions, JsonConsoleOptions? consoleOptions = null)
        where TOptions : ClientPipelineOptions
    {
        consoleOptions ??= JsonConsoleOptions.Default;

        if (consoleOptions.InteractiveConfirm && ConsoleExtensions.IsConsoleInteractive && !AnsiConsole.Confirm("Do you want to enable rich JSON console logging for HTTP pipeline messages?"))
            return pipelineOptions;

        if (consoleOptions.InteractiveOnly && !ConsoleExtensions.IsConsoleInteractive)
            return pipelineOptions;

        pipelineOptions.Transport = new ConsoleLoggingPipelineTransport(pipelineOptions.Transport ?? HttpClientPipelineTransport.Shared, consoleOptions);

        return pipelineOptions;
    }

    /// <summary>
    /// Renders chat messages and responses to the console using Spectre.Console rich JSON formatting.
    /// </summary>
    /// <param name="builder">The builder in use.</param>
    /// <remarks>
    /// Confirmation will be asked if the console is interactive, otherwise, it will be 
    /// enabled unconditionally.
    /// </remarks>
    public static ChatClientBuilder UseJsonConsoleLogging(this ChatClientBuilder builder, JsonConsoleOptions? consoleOptions = null)
    {
        consoleOptions ??= JsonConsoleOptions.Default;

        if (consoleOptions.InteractiveConfirm && ConsoleExtensions.IsConsoleInteractive && !AnsiConsole.Confirm("Do you want to enable rich JSON console logging for HTTP pipeline messages?"))
            return builder;

        if (consoleOptions.InteractiveOnly && !ConsoleExtensions.IsConsoleInteractive)
            return builder;

        return builder.Use(inner => new JsonConsoleLoggingChatClient(inner, consoleOptions));
    }

    class ConsoleLoggingPipelineTransport(PipelineTransport inner, JsonConsoleOptions consoleOptions) : PipelineTransport
    {
        public static PipelineTransport Default { get; } = new ConsoleLoggingPipelineTransport();

        public ConsoleLoggingPipelineTransport() : this(HttpClientPipelineTransport.Shared, JsonConsoleOptions.Default) { }

        protected override async ValueTask ProcessCoreAsync(PipelineMessage message)
        {
            message.BufferResponse = true;
            await inner.ProcessAsync(message);

            if (message.Request.Content is not null)
            {
                using var memory = new MemoryStream();
                message.Request.Content.WriteTo(memory);
                memory.Position = 0;
                using var reader = new StreamReader(memory);
                var content = await reader.ReadToEndAsync();
                AnsiConsole.Write(consoleOptions.CreatePanel(content));
            }

            if (message.Response != null)
            {
                AnsiConsole.Write(consoleOptions.CreatePanel(message.Response.Content.ToString()));
            }
        }

        protected override PipelineMessage CreateMessageCore() => inner.CreateMessage();
        protected override void ProcessCore(PipelineMessage message) => inner.Process(message);
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
