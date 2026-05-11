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
        builder.Services.AddChatClients(builder.Configuration, prefix, useDefaultProviders);
        builder.Services.AddClients(builder.Configuration, prefix, useDefaultProviders);
        return builder;
    }

    /// <summary>
    /// Adds configuration-driven AI clients to the host application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configureChat">Optional action to configure each chat client.</param>
    /// <param name="configureTTS">Optional action to configure each text-to-speech client.</param>
    /// <param name="configureSTT">Optional action to configure each speech-to-text client.</param>
    /// <param name="prefix">The configuration prefix for clients. Defaults to "ai:clients".</param>
    /// <param name="useDefaultProviders">Whether to register the default built-in <see cref="IClientProvider"/> providers for mapping configuration sections to clients.</param>
    /// <returns>The host application builder.</returns>
    [Obsolete("Use ConfigureChatClientDefaults/ConfigureTextToSpeechClientDefaults/ConfigureSpeechToTextClientDefaults instead.")]
    public static TBuilder AddAIClients<TBuilder>(this TBuilder builder,
        Action<string, ChatClientBuilder> configureChat,
        Action<string, TextToSpeechClientBuilder>? configureTTS = default,
        Action<string, SpeechToTextClientBuilder>? configureSTT = default,
        string prefix = "ai:clients", bool useDefaultProviders = true)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureChat);

        builder.Services.AddChatClients(builder.Configuration, (id, b) => configureChat(id, b), prefix, useDefaultProviders);
        builder.Services.AddClients(builder.Configuration, prefix, useDefaultProviders);

        if (configureTTS != null)
            builder.Services.AddSingleton(new TextToSpeechDefaultsEntry(null, b => configureTTS(string.Empty, b)));

        if (configureSTT != null)
            builder.Services.AddSingleton(new SpeechToTextDefaultsEntry(null, b => configureSTT(string.Empty, b)));

        return builder;
    }
}
