using Microsoft.Extensions.AI;
using OpenAI.Chat;

/// <summary>
/// Smarter casting to <see cref="IChatClient"/> when the target <see cref="ChatClient"/> 
/// already implements the interface.
/// </summary>
static class ChatClientExtensions
{
#if NET9_0_OR_GREATER
    [System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute(100)]
#endif
    /// <summary>Gets an <see cref="IChatClient"/> for use with this <see cref="ChatClient"/>.</summary>
    public static IChatClient AsIChatClient(this ChatClient client) =>
        client as IChatClient ?? OpenAIClientExtensions.AsIChatClient(client);
}