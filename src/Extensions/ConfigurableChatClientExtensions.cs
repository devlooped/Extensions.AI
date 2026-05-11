using System.ComponentModel;
using Devlooped.Extensions.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Adds configuration-driven chat clients to an application host or service collection.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConfigurableChatClientExtensions
{
    /// <summary>
    /// Adds configuration-driven chat clients to the host application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="prefix">The configuration prefix for clients. Defaults to "ai:clients".</param>
    /// <param name="useDefaultProviders">Whether to register the default built-in <see cref="IClientProvider"/> providers for mapping configuration sections to <see cref="IChatClient"/> instances.</param>
    /// <returns>The host application builder.</returns>
    public static TBuilder AddChatClients<TBuilder>(this TBuilder builder, string prefix = "ai:clients", bool useDefaultProviders = true)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        AddChatClientsCore(builder.Services, builder.Configuration, configure: null, prefix, useDefaultProviders);
        return builder;
    }

    /// <summary>
    /// Adds configuration-driven chat clients to the host application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">Optional action to configure the pipeline for each client.</param>
    /// <param name="prefix">The configuration prefix for clients. Defaults to "ai:clients".</param>
    /// <param name="useDefaultProviders">Whether to register the default built-in <see cref="IClientProvider"/> providers for mapping configuration sections to <see cref="IChatClient"/> instances.</param>
    /// <returns>The host application builder.</returns>
    [Obsolete("Use ConfigureChatClientDefaults instead of the configure parameter. This overload will be removed in a future version.")]
    public static TBuilder AddChatClients<TBuilder>(this TBuilder builder, Action<string, ChatClientBuilder> configure, string prefix = "ai:clients", bool useDefaultProviders = true)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);
        AddChatClientsCore(builder.Services, builder.Configuration, configure, prefix, useDefaultProviders);
        return builder;
    }

    /// <summary>
    /// Adds configuration-driven chat clients to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="prefix">The configuration prefix for clients. Defaults to "ai:clients".</param>
    /// <param name="useDefaultProviders">Whether to register the default built-in <see cref="IClientProvider"/> providers for mapping configuration sections to <see cref="IChatClient"/> instances.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddChatClients(this IServiceCollection services, IConfiguration configuration, string prefix = "ai:clients", bool useDefaultProviders = true)
    {
        AddChatClientsCore(services, configuration, configure: null, prefix, useDefaultProviders);
        return services;
    }

    /// <summary>
    /// Adds configuration-driven chat clients to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configure">Optional action to configure the pipeline for each client.</param>
    /// <param name="prefix">The configuration prefix for clients. Defaults to "ai:clients".</param>
    /// <param name="useDefaultProviders">Whether to register the default built-in <see cref="IClientProvider"/> providers for mapping configuration sections to <see cref="IChatClient"/> instances.</param>
    /// <returns>The service collection.</returns>
    [Obsolete("Use ConfigureChatClientDefaults instead of the configure parameter. This overload will be removed in a future version.")]
    public static IServiceCollection AddChatClients(this IServiceCollection services, IConfiguration configuration, Action<string, ChatClientBuilder> configure, string prefix = "ai:clients", bool useDefaultProviders = true)
    {
        ArgumentNullException.ThrowIfNull(configure);
        AddChatClientsCore(services, configuration, configure, prefix, useDefaultProviders);
        return services;
    }

    /// <summary>Gets a chat client by id (case-insensitive) from the service provider.</summary>
    public static IChatClient? GetChatClient(this IServiceProvider services, string id)
        => services.GetKeyedService<IChatClient>(id) ?? services.GetKeyedService<IChatClient>(new ServiceKey(id));

    /// <summary>Gets a text to speech client by id (case-insensitive) from the service provider.</summary>
    public static ITextToSpeechClient? GetTextToSpeechClient(this IServiceProvider services, string id)
        => services.GetKeyedService<ITextToSpeechClient>(id) ?? services.GetKeyedService<ITextToSpeechClient>(new ServiceKey(id));

    /// <summary>Gets a speech to text client by id (case-insensitive) from the service provider.</summary>
    public static ISpeechToTextClient? GetSpeechToTextClient(this IServiceProvider services, string id)
        => services.GetKeyedService<ISpeechToTextClient>(id) ?? services.GetKeyedService<ISpeechToTextClient>(new ServiceKey(id));

    static void AddChatClientsCore(IServiceCollection services, IConfiguration configuration, Action<string, ChatClientBuilder>? configure, string prefix, bool useDefaultProviders)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Ensure the factory and providers are registered
        services.AddClientFactory(useDefaultProviders);

        foreach (var entry in configuration.AsEnumerable().Where(x =>
            x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            x.Key.EndsWith("modelid", StringComparison.OrdinalIgnoreCase)))
        {
            var section = string.Join(':', entry.Key.Split(':')[..^1]);
            // ID == section after agents:, with optional overridable id
            var id = configuration[$"{section}:id"] ?? section[(prefix.Length + 1)..];

            var options = configuration.GetRequiredSection(section).Get<ChatClientOptions>();
            // We need logging set up for the configurable client to log changes
            services.AddLogging();

            services.TryAdd(new ServiceDescriptor(typeof(IChatClient), id,
                factory: (sp, _) =>
                {
                    var client = new ConfigurableChatClient(configuration,
                        sp.GetRequiredService<IClientFactoryResolver>(),
                        sp.GetRequiredService<ILogger<ConfigurableChatClient>>(),
                        section, id);

                    var entries = sp.GetServices<ChatDefaultsEntry>()
                        .Where(e => e.SectionPath == null ||
                            string.Equals(e.SectionPath, section, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (entries.Count == 0 && configure == null)
                        return client;

                    var builder = client.AsBuilder();
                    foreach (var defaults in entries)
                        defaults.Configure(builder);

                    configure?.Invoke(id, builder);

                    return builder.Build(sp);
                },
                options?.Lifetime ?? ServiceLifetime.Singleton));

            services.TryAdd(new ServiceDescriptor(typeof(IChatClient), new ServiceKey(id),
                factory: (sp, _) => sp.GetRequiredKeyedService<IChatClient>(id),
                options?.Lifetime ?? ServiceLifetime.Singleton));
        }
    }

    internal class ChatClientOptions : OpenAIClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;
    }
}
