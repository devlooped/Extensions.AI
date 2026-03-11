using System.Collections;
using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Collection of <see cref="ChatMessage"/> for more convenient usage 
/// in fluent construction of chat messages.
/// </summary>
[DebuggerTypeProxy(typeof(ChatDebugView))]
[DebuggerDisplay("Count = {messages.Count}")]
public class Chat : IEnumerable<ChatMessage>
{
    readonly List<ChatMessage> messages = [];

    /// <summary>
    /// Adds a message to the list of chat messages. 
    /// For use with collection initializer syntax.
    /// </summary>
    public void Add(ChatMessage message) => messages.Add(message);

    /// <summary>Adds a range of messages to the list of chat messages.</summary>
    /// <param name="messages">The messages to add</param>
    public void AddRange(IEnumerable<ChatMessage> messages) => this.messages.AddRange(messages);

    /// <summary>
    /// Adds a message to the list of chat messages.
    /// </summary>
    /// <param name="role">The message role</param>
    /// <param name="message">The message text</param>
    /// <remarks>
    /// Example usage:
    /// <code>
    /// var messages = new Chat()
    /// {
    ///     { "system", "You are a highly intelligent AI assistant." },
    ///     { "user", "What is 101*3?" },
    /// };
    /// </code>
    /// </remarks>
    public void Add(string role, string message) => Add(
        new ChatMessage(role.ToLowerInvariant() switch
        {
            "system" => ChatRole.System,
            "assistant" => ChatRole.Assistant,
            "user" => ChatRole.User,
            _ => new ChatRole(role)
        }, message));

    /// <summary>Creates a user message.</summary>
    public static ChatMessage User(string message) => new(ChatRole.User, message);

    /// <summary>Creates an assistant message.</summary>
    public static ChatMessage Assistant(string message) => new(ChatRole.Assistant, message);

    /// <summary>Creates a system message.</summary>
    public static ChatMessage System(string message) => new(ChatRole.System, message);

    /// <summary>Creates a developer message.</summary>
    public static ChatMessage Developer(string message) => new(new("developer"), message);

    IEnumerator<ChatMessage> IEnumerable<ChatMessage>.GetEnumerator() => messages.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => messages.GetEnumerator();

    sealed class ChatDebugView(Chat chat)
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ChatMessage[] Items
        {
            get
            {
                var items = new ChatMessage[chat.messages.Count];
                chat.messages.CopyTo(items, 0);
                return items;
            }
        }
    }
}