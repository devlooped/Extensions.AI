using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Provides usability overloads for the <see cref="IChatClient"/> interface.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ChatExtensions
{
    extension(IChatClient client)
    {
        /// <summary>
        /// Allows passing a <see cref="Chat"/> instance to the chat client
        /// </summary>
        /// <param name="chat">The chat messages in a single object.</param>
        /// <param name="options">The optional chat options.</param>
        /// <param name="cancellation">Optional cancellation token.</param>
        public Task<ChatResponse> GetResponseAsync(Chat chat, ChatOptions? options = null, CancellationToken cancellation = default)
            => client.GetResponseAsync((IEnumerable<ChatMessage>)chat, options, cancellation);
    }

    extension(ChatOptions options)
    {
        /// <summary>
        /// Sets the effort level for a reasoning AI model when generating responses, if supported 
        /// by the model.
        /// </summary>
        public ReasoningEffort? ReasoningEffort
        {
            get => options.AdditionalProperties?.TryGetValue("reasoning_effort", out var value) == true && value is ReasoningEffort effort ? effort : null;
            set
            {
                if (value is not null)
                {
                    options.AdditionalProperties ??= [];
                    options.AdditionalProperties["reasoning_effort"] = value;
                }
                else
                {
                    options.AdditionalProperties?.Remove("reasoning_effort");
                }
            }
        }

        /// <summary>
        /// Sets the <see cref="Verbosity"/> level for a GPT-5 model when generating responses, if supported
        /// </summary>
        public Verbosity? Verbosity
        {
            get => options.AdditionalProperties?.TryGetValue("verbosity", out var value) == true && value is Verbosity verbosity ? verbosity : null;
            set
            {
                if (value is not null)
                {
                    options.AdditionalProperties ??= [];
                    options.AdditionalProperties["verbosity"] = value;
                }
                else
                {
                    options.AdditionalProperties?.Remove("verbosity");
                }
            }
        }
    }
}

// Workaround to get the config binder to set these extension properties.
/// <summary>
/// Defines extended <see cref="ChatOptions"/> we provide via extension properties.
/// </summary>
/// <devdoc>This should ideally even be auto-generated from the available extensions so it's always in sync.</devdoc>
[EditorBrowsable(EditorBrowsableState.Never)]
public class ExtendedChatOptions : ChatOptions
{
    public ReasoningEffort? ReasoningEffort
    {
        get => ((ChatOptions)this).ReasoningEffort;
        set => ((ChatOptions)this).ReasoningEffort = value;
    }
    public Verbosity? Verbosity
    {
        get => ((ChatOptions)this).Verbosity;
        set => ((ChatOptions)this).Verbosity = value;
    }
}