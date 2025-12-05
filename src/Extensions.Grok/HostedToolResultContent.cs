using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

/// <summary>Represents a hosted tool agentic call.</summary>
/// <param name="toolCall">The tool call details.</param>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
[Experimental("DEAI001")]
public class HostedToolResultContent : AIContent
{
    /// <summary>Gets or sets the tool call ID.</summary>
    public virtual string? CallId { get; set; }

    /// <summary>Gets or sets the resulting contents from the tool.</summary>
    public virtual IList<AIContent>? Outputs { get; set; }
}