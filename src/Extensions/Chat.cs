using System.Collections;
using System.Diagnostics;
using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

/// <summary>Collection of <see cref="ChatMessage"/> for more convenient usage in fluent construction of chat messages.</summary>
[DebuggerTypeProxy(typeof(ChatDebugView))]
[DebuggerDisplay("Count = {messages.Count}")]
public class Chat : IList<ChatMessage>
{
    readonly List<ChatMessage> messages = [];

    /// <summary>Gets or sets the message at the specified index.</summary>
    public ChatMessage this[int index]
    {
        get => messages[index];
        set => messages[index] = value;
    }

    /// <summary>Gets the number of messages in the chat.</summary>
    public int Count => messages.Count;

    /// <summary>Gets a value indicating whether the chat is read-only.</summary>
    public bool IsReadOnly => ((ICollection<ChatMessage>)messages).IsReadOnly;

    /// <summary>Adds a message to the list of chat messages. For use with collection initializer syntax.</summary>
    public void Add(ChatMessage message) => messages.Add(message);

    /// <summary>Clears all messages from the chat.</summary>
    public void Clear() => messages.Clear();

    /// <summary>Determines whether the chat contains the specified message.</summary>
    public bool Contains(ChatMessage item) => messages.Contains(item);

    /// <summary>Copies the messages to the specified array.</summary>
    public void CopyTo(ChatMessage[] array, int arrayIndex) => messages.CopyTo(array, arrayIndex);

    /// <summary>Adds a range of messages to the list of chat messages.</summary>
    /// <param name="messages">The messages to add</param>
    public void AddRange(IEnumerable<ChatMessage> messages) => this.messages.AddRange(messages);

    /// <summary>Adds a message to the list of chat messages.</summary>
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

    /// <summary>Returns an enumerator that iterates through the chat messages.</summary>
    public IEnumerator<ChatMessage> GetEnumerator() => messages.GetEnumerator();

    /// <summary>Returns the index of the specified message.</summary>
    public int IndexOf(ChatMessage item) => messages.IndexOf(item);

    /// <summary>Inserts a message at the specified index.</summary>
    public void Insert(int index, ChatMessage item) => messages.Insert(index, item);

    /// <summary>Removes the specified message from the chat.</summary>
    public bool Remove(ChatMessage item) => messages.Remove(item);

    /// <summary>Removes the message at the specified index.</summary>
    public void RemoveAt(int index) => messages.RemoveAt(index);

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
