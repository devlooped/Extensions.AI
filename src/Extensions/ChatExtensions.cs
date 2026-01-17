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
        /// <summary>Gets or sets the end user ID for the chat session.</summary>
        public string? EndUserId
        {
            get => (options.AdditionalProperties ??= []).TryGetValue("EndUserId", out var value) ? value as string : null;
            set => (options.AdditionalProperties ??= [])["EndUserId"] = value;
        }
    }
}