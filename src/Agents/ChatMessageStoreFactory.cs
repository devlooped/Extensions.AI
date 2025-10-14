using Microsoft.Agents.AI;
using static Microsoft.Agents.AI.ChatClientAgentOptions;

namespace Devlooped.Agents.AI;

/// <summary>
/// An implementation of a <see cref="ChatMessageStore"/> factory as a class that can provide 
/// the functionality to <see cref="ChatClientAgentOptions.ChatMessageStoreFactory"/> and integrates 
/// more easily into a service collection.
/// </summary>
/// <remarks>
/// The <see cref="ChatMessageStore"/> is a key extensibility point in Microsoft.Agents.AI, allowing 
/// storage and retrieval of chat messages.
/// </remarks>
public abstract class ChatMessageStoreFactory
{
    /// <summary>
    /// Provides the implementation of <see cref="ChatClientAgentOptions.ChatMessageStoreFactory"/> 
    /// to provide message persistence.
    /// </summary>
    /// <param name="context">The context to potentially hydrate state from.</param>
    /// <returns>The message store that will handle chat messages.</returns>
    public abstract ChatMessageStore CreateStore(ChatMessageStoreFactoryContext context);
}
