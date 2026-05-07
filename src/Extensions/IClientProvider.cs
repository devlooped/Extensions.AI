namespace Devlooped.Extensions.AI;

/// <summary>
/// Represents a provider that can create AI client instances for a specific AI service.
/// </summary>
public interface IClientProvider
{
    /// <summary>
    /// Gets the unique name of this provider (e.g., "openai", "azure.openai", "azure.inference", "xai").
    /// </summary>
    /// <remarks>
    /// This name is used for explicit provider selection via the <c>provider</c> configuration key.
    /// </remarks>
    string ProviderName { get; }

    /// <summary>
    /// Gets the base URI prefix used for automatic provider detection, if any.
    /// </summary>
    /// <remarks>
    /// When a configuration section does not specify an explicit <c>provider</c> key,
    /// the factory will match the <c>endpoint</c> against registered providers' base URIs.
    /// The URI can include path segments for more specific matching (e.g., <c>https://api.x.ai/v1</c>).
    /// </remarks>
    Uri? BaseUri { get; }

    /// <summary>
    /// Gets the host suffix pattern used for automatic provider detection, if any.
    /// </summary>
    /// <remarks>
    /// When a configuration section does not specify an explicit <c>provider</c> key,
    /// the factory will match the <c>endpoint</c> host against registered providers' host suffixes.
    /// For example, <c>.openai.azure.com</c> matches any Azure OpenAI endpoint.
    /// If both <see cref="BaseUri"/> and <see cref="HostSuffix"/> are provided,
    /// <see cref="BaseUri"/> is tested first.
    /// </remarks>
    string? HostSuffix { get; }

    /// <summary>Gets the provider-specific factory that knows how to create individual clients.</summary>
    IClientFactory GetFactory();
}
