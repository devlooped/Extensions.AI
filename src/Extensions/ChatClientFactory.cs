using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Default implementation of <see cref="IChatClientFactory"/> that resolves providers
/// by name or by matching endpoint URIs.
/// </summary>
public class ChatClientFactory : IChatClientFactory
{
    readonly IChatClientProvider defaultProvider = new OpenAIChatClientProvider();

    readonly Dictionary<string, IChatClientProvider> providersByName;
    readonly List<(Uri BaseUri, IChatClientProvider Provider)> providersByBaseUri;
    readonly List<(string HostSuffix, IChatClientProvider Provider)> providersByHostSuffix;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatClientFactory"/> class
    /// with the specified providers.
    /// </summary>
    /// <param name="providers">The collection of registered providers.</param>
    public ChatClientFactory(IEnumerable<IChatClientProvider> providers)
    {
        providersByName = new(StringComparer.OrdinalIgnoreCase);
        providersByBaseUri = [];
        providersByHostSuffix = [];

        foreach (var provider in providers)
        {
            providersByName[provider.ProviderName] = provider;
            if (provider.BaseUri is { } baseUri)
                providersByBaseUri.Add((baseUri, provider));

            // Register host suffix for providers that use suffix matching
            if (provider.HostSuffix is { } hostSuffix)
                providersByHostSuffix.Add((hostSuffix, provider));
        }

        // Sort by URI length descending for longest-prefix matching
        providersByBaseUri.Sort((a, b) => b.BaseUri.ToString().Length.CompareTo(a.BaseUri.ToString().Length));
        // Sort by suffix length descending for longest-suffix matching
        providersByHostSuffix.Sort((a, b) => b.HostSuffix.Length.CompareTo(a.HostSuffix.Length));
    }

    /// <summary>
    /// Creates a <see cref="ChatClientFactory"/> with the built-in providers registered.
    /// </summary>
    /// <returns>A factory with OpenAI, Azure OpenAI, Azure AI Inference, and Grok providers.</returns>
    public static ChatClientFactory CreateDefault() => new(
    [
        new OpenAIChatClientProvider(),
        new AzureOpenAIChatClientProvider(),
        new AzureAIInferenceChatClientProvider(),
        new GrokChatClientProvider(),
    ]);

    /// <inheritdoc/>
    public IChatClient CreateClient(IConfigurationSection section)
        => ResolveProvider(section).Create(section);

    /// <summary>
    /// Resolves the appropriate provider for the given configuration section.
    /// </summary>
    /// <param name="section">The configuration section.</param>
    /// <returns>The resolved provider.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no matching provider is found.
    /// </exception>
    protected virtual IChatClientProvider ResolveProvider(IConfigurationSection section)
    {
        // First, try explicit provider name
        var providerName = section["provider"];
        if (!string.IsNullOrEmpty(providerName))
        {
            if (providersByName.TryGetValue(providerName, out var namedProvider))
                return namedProvider;

            throw new InvalidOperationException(
                $"No chat client provider registered with name '{providerName}'. " +
                $"Available providers: {string.Join(", ", providersByName.Keys)}.");
        }

        // Second, try endpoint URI matching
        var endpointValue = section["endpoint"];
        if (!string.IsNullOrEmpty(endpointValue) && Uri.TryCreate(endpointValue, UriKind.Absolute, out var endpoint))
        {
            // Try base URI prefix matching first
            foreach (var (baseUri, provider) in providersByBaseUri)
            {
                if (MatchesBaseUri(endpoint, baseUri))
                    return provider;
            }

            // Then try host suffix matching (e.g., .openai.azure.com)
            foreach (var (hostSuffix, provider) in providersByHostSuffix)
            {
                if (endpoint.Host.EndsWith(hostSuffix, StringComparison.OrdinalIgnoreCase))
                    return provider;
            }
        }

        if (string.IsNullOrEmpty(endpointValue))
            return defaultProvider;

        throw new InvalidOperationException(
            $"No chat client provider found for configuration section '{section.Path}'. " +
            $"Specify a 'provider' key or use an 'endpoint' that matches a registered provider. " +
            $"Available providers: {string.Join(", ", providersByName.Keys)}.");
    }

    /// <summary>
    /// Determines if the endpoint URI matches the provider's base URI pattern.
    /// </summary>
    /// <param name="endpoint">The endpoint URI from configuration.</param>
    /// <param name="baseUri">The provider's base URI pattern.</param>
    /// <returns><c>true</c> if the endpoint matches the pattern; otherwise, <c>false</c>.</returns>
    protected virtual bool MatchesBaseUri(Uri endpoint, Uri baseUri)
    {
        // Check host match
        if (!string.Equals(endpoint.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase))
            return false;

        // Check scheme
        if (!string.Equals(endpoint.Scheme, baseUri.Scheme, StringComparison.OrdinalIgnoreCase))
            return false;

        // Check path prefix (for patterns like api.x.ai/v1)
        var basePath = baseUri.AbsolutePath.TrimEnd('/');
        if (!string.IsNullOrEmpty(basePath) && basePath != "/")
        {
            var endpointPath = endpoint.AbsolutePath;
            if (!endpointPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}
