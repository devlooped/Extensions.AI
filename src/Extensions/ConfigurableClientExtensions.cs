using System.ComponentModel;
using Devlooped.Extensions.AI;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Adds configuration-driven AI clients to an application host or service collection.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConfigurableClientExtensions
{
    /// <summary>
    /// Adds configuration-driven AI clients to the host application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="prefix">The configuration prefix for clients. Defaults to "ai:clients".</param>
    /// <param name="useDefaultProviders">Whether to register the default built-in <see cref="IClientProvider"/> providers for mapping configuration sections to clients.</param>
    /// <returns>The host application builder.</returns>
    public static TBuilder AddAIClients<TBuilder>(this TBuilder builder,
        string prefix = "ai:clients", bool useDefaultProviders = true)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddClients(builder.Configuration, prefix, useDefaultProviders);
        return builder;
    }
}
