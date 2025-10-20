using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Devlooped.Agents.AI;

/// <summary>
/// Miscenalleous extension methods for agents.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AgentExtensions
{
    /// <summary>Ensures the returned <see cref="ChatResponse"/> contains the <see cref="AgentRunResponse.AgentId"/> as an additional property.</summary>
    /// <devdoc>Change priority to -10 and make EditorBrowsable.Never when https://github.com/microsoft/agent-framework/issues/1574 is fixed.</devdoc>
    [OverloadResolutionPriority(10)]
    public static ChatResponse AsChatResponse(this AgentRunResponse response)
    {
        var chatResponse = AgentRunResponseExtensions.AsChatResponse(response);

        chatResponse.AdditionalProperties ??= [];
        chatResponse.AdditionalProperties[nameof(response.AgentId)] = response.AgentId;

        return chatResponse;
    }
}