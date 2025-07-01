using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

public enum GrokSearch { Auto, On, Off }

/// <summary>
/// Configures Grok's live search capabilities. 
/// See https://docs.x.ai/docs/guides/live-search.
/// </summary>
public class GrokSearchTool(GrokSearch mode) : HostedWebSearchTool
{
    /// <summary>
    /// Sets the search mode for Grok's live search capabilities.
    /// </summary>
    public GrokSearch Mode { get; } = mode;

    public override string Name => "Live Search";

    public override string Description => "Performs live search using X.AI";
}
