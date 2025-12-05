using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

/// <summary>Represents a hosted tool agentic call.</summary>
/// <param name="toolCall">The tool call details.</param>
[Experimental("DEAI001")]
public class HostedToolCallContent : AIContent
{
    /// <summary>Gets or sets the tool call ID.</summary>
    public virtual string? CallId { get; set; }
}
