using Microsoft.Agents.AI;
using static Microsoft.Agents.AI.ChatClientAgentOptions;

namespace Devlooped.Agents.AI;

/// <summary>
/// An implementation of an <see cref="AIContextProvider"/> factory as a class that can provide 
/// the functionality to <see cref="ChatClientAgentOptions.AIContextProviderFactory"/> and integrates 
/// more easily into a service collection.
/// </summary>
/// <remarks>
/// The <see cref="AIContextProvider"/> is a key extensibility point in Microsoft.Agents.AI, allowing 
/// augmentation of instructions, messages and tools before agent execution is performed.
/// </remarks>
public abstract class AIContextProviderFactory
{
    /// <summary>
    /// Provides the implementation of <see cref="ChatClientAgentOptions.AIContextProviderFactory"/>, 
    /// which is invoked whenever agent threads are created or rehydrated.
    /// </summary>
    /// <param name="context">The context to potentially hydrate state from.</param>
    /// <returns>The context provider that will enhance interactions with an agent.</returns>
    public abstract AIContextProvider CreateProvider(AIContextProviderFactoryContext context);
}
