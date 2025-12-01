using System.Diagnostics;
using System.Text.Json.Serialization;
using Devlooped.Grok;
using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

/// <summary>Represents a hosted tool agentic call.</summary>
/// <param name="toolCall">The tool call details.</param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[method: JsonConstructor]
public sealed class HostedToolCallContent(ToolCall toolCall) : AIContent
{
    /// <summary>Gets the tool call details.</summary>
    public ToolCall ToolCall => toolCall;

    /// <summary>Gets a string representing this instance to display in the debugger.</summary>
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    string DebuggerDisplay
    {
        get
        {
            var display = $"ToolCall = {toolCall.Id}, ";

            display += toolCall.Function.Arguments is not null ?
                $"{toolCall.Function.Name}({toolCall.Function.Arguments})" :
                $"{toolCall.Function.Name}()";

            return display;
        }
    }
}
