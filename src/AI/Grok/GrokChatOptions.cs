using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Grok-specific chat options that extend the base <see cref="ChatOptions"/>
/// with <see cref="Search"/> and <see cref="ReasoningEffort"/> properties.
/// </summary>
public class GrokChatOptions : ChatOptions
{
    /// <summary>
    /// Configures Grok's live search capabilities. 
    /// See https://docs.x.ai/docs/guides/live-search.
    /// </summary>
    public GrokSearch Search { get; set; } = GrokSearch.Auto;

    /// <summary>
    /// Configures the reasoning effort level for Grok's responses.
    /// See https://docs.x.ai/docs/guides/reasoning.
    /// </summary>
    public ReasoningEffort? ReasoningEffort { get; set; }
}
