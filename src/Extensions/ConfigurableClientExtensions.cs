using System.ComponentModel;
using Devlooped.Extensions.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Adds configuration-driven AI clients to an application host or service collection.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConfigurableClientExtensions
{
    /// <summary>
    /// Adds configuration-driven chat clients to the host application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configurePipeline">Optional action to configure the pipeline for each client.</param>
    /// <param name="configureClient">Optional action to configure each client.</param>
    /// <param name="prefix">The configuration prefix for clients. Defaults to "ai:clients".</param>
    /// <param name="useDefaultProviders">Whether to register the default built-in <see cref="IClientProvider"/> providers for mapping configuration sections to <see cref="IChatClient"/> instances.</param>
    /// <returns>The host application builder.</returns>
    public static TBuilder AddAIClients<TBuilder>(this TBuilder builder,
        Action<string, ChatClientBuilder>? configureChat = default,
        Action<string, TextToSpeechClientBuilder>? configureTTS = default,
        Action<string, SpeechToTextClientBuilder>? configureSTT = default,
        string prefix = "ai:clients", bool useDefaultProviders = true)
        where TBuilder : IHostApplicationBuilder
    {
        return builder;
    }
}
