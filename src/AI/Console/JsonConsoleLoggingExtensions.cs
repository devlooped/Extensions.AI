using System.ClientModel.Primitives;
using System.ComponentModel;
using Devlooped.Extensions.AI;
using Spectre.Console;
using Spectre.Console.Json;

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
    /// <param name="options">The options instance to configure.</param>
    /// <remarks>
    /// NOTE: this is the lowst-level logging after all chat pipeline processing has been done.
    /// <para>
    /// If the options already provide a transport, it will be wrapped with the console 
    /// logging transport to minimize the impact on existing configurations.
    /// </para>
    /// </remarks>
    public static TOptions UseJsonConsoleLogging<TOptions>(this TOptions options)
        where TOptions : ClientPipelineOptions
        => UseJsonConsoleLogging(options, ConsoleExtensions.IsConsoleInteractive);

    /// <summary>
    /// Sets a <see cref="ClientPipelineOptions.Transport"/> that renders HTTP messages to the 
    /// console using Spectre.Console rich JSON formatting.
    /// </summary>
    /// <typeparam name="TOptions">The options type to configure for HTTP logging.</typeparam>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="askConfirmation">Whether to confirm logging before enabling it.</param>
    /// <remarks>
    /// NOTE: this is the lowst-level logging after all chat pipeline processing has been done.
    /// <para>
    /// If the options already provide a transport, it will be wrapped with the console 
    /// logging transport to minimize the impact on existing configurations.
    /// </para>
    /// </remarks>
    public static TOptions UseJsonConsoleLogging<TOptions>(this TOptions options, bool askConfirmation)
        where TOptions : ClientPipelineOptions
    {
        if (askConfirmation && !AnsiConsole.Confirm("Do you want to enable rich JSON console logging for HTTP pipeline messages?"))
            return options;

        options.Transport = new ConsoleLoggingPipelineTransport(options.Transport ?? HttpClientPipelineTransport.Shared);

        return options;
    }

    /// <summary>
    /// Renders chat messages and responses to the console using Spectre.Console rich JSON formatting.
    /// </summary>
    /// <param name="builder">The builder in use.</param>
    /// <remarks>
    /// Confirmation will be asked if the console is interactive, otherwise, it will be 
    /// enabled unconditionally.
    /// </remarks>
    public static ChatClientBuilder UseJsonConsoleLogging(this ChatClientBuilder builder)
        => UseJsonConsoleLogging(builder, ConsoleExtensions.IsConsoleInteractive);

    /// <summary>
    /// Renders chat messages and responses to the console using Spectre.Console rich JSON formatting.
    /// </summary>
    /// <param name="builder">The builder in use.</param>
    /// <param name="askConfirmation">If true, prompts the user for confirmation before enabling console logging.</param>
    /// <param name="maxLength">Optional maximum length to render for string values. Replaces remaining characters with "...".</param>
    public static ChatClientBuilder UseJsonConsoleLogging(this ChatClientBuilder builder, bool askConfirmation = false, Action<JsonConsoleLoggingChatClient>? configure = null)
    {
        if (askConfirmation && !AnsiConsole.Confirm("Do you want to enable console logging for chat messages?"))
            return builder;

        return builder.Use(inner =>
        {
            var client = new JsonConsoleLoggingChatClient(inner);
            configure?.Invoke(client);
            return client;
        });
    }

    class ConsoleLoggingPipelineTransport(PipelineTransport inner) : PipelineTransport
    {
        public static PipelineTransport Default { get; } = new ConsoleLoggingPipelineTransport();

        public ConsoleLoggingPipelineTransport() : this(HttpClientPipelineTransport.Shared) { }

        protected override PipelineMessage CreateMessageCore() => inner.CreateMessage();
        protected override void ProcessCore(PipelineMessage message) => inner.Process(message);

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

                AnsiConsole.Write(new Panel(new JsonText(content)));
            }

            if (message.Response != null)
            {
                AnsiConsole.Write(new Panel(new JsonText(message.Response.Content.ToString())));
            }
        }
    }

}
