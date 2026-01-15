using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace Devlooped.Extensions.AI;

/// <summary>
/// A factory for creating <see cref="IChatClient"/> instances based on configuration.
/// </summary>
/// <remarks>
/// The factory resolves the appropriate <see cref="IChatClientProvider"/> using the following logic:
/// <list type="number">
///   <item><description>If the configuration section contains a <c>provider</c> key, looks up a provider by name.</description></item>
///   <item><description>Otherwise, matches the <c>endpoint</c> URI against registered providers' base URIs or host suffix, if any.</description></item>
///   <item><description>If no match is found, throws an <see cref="InvalidOperationException"/>.</description></item>
/// </list>
/// </remarks>
public interface IChatClientFactory
{
    /// <summary>
    /// Creates an <see cref="IChatClient"/> using the specified configuration section.
    /// </summary>
    /// <param name="section">The configuration section containing client settings including 
    /// <c>endpoint</c>, <c>apikey</c>, <c>modelid</c>, and optionally <c>provider</c>.</param>
    /// <returns>A configured <see cref="IChatClient"/> instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no matching provider is found for the given configuration.
    /// </exception>
    IChatClient CreateClient(IConfigurationSection section);
}
