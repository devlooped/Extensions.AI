using Microsoft.Extensions.Configuration;

namespace Devlooped.Extensions.AI;

/// <summary>Resolves providers by name or by matching endpoint URIs, then returns section-bound factories.</summary>
interface IClientFactoryResolver
{
    /// <summary>Resolves the appropriate provider for the given configuration section and returns its bound factory.</summary>
    /// <param name="section">The configuration section containing client settings.</param>
    /// <returns>A provider-specific <see cref="IClientFactory"/> bound to <paramref name="section"/>.</returns>
    IClientFactory Resolve(IConfigurationSection section);
}