using System.Collections;
using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Collection of <see cref="ChatMessage"/> for more convenient usage 
/// in fluent construction of chat messages.
/// </summary>
public class Chat : IEnumerable<ChatMessage>
{
    readonly List<ChatMessage> messages = [];

    /// <summary>
    /// Adds a message to the list of chat messages. 
    /// For use with collection initializer syntax.
    /// </summary>
    public void Add(ChatMessage message) => messages.Add(message);

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
    public void Add(string role, string message)
        => messages.Add(new ChatMessage(role.ToLowerInvariant() switch
        {
            "system" => ChatRole.System,
            "assistant" => ChatRole.Assistant,
            "user" => ChatRole.User,
            _ => new ChatRole(role)
        }, message));

    IEnumerator<ChatMessage> IEnumerable<ChatMessage>.GetEnumerator() => messages.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => messages.GetEnumerator();
}