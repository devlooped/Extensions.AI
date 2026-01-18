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
    /// <param name="configurePipeline">Optional action to configure the pipeline for each client.</param>
    /// <param name="configureClient">Optional action to configure each client.</param>
    /// <param name="prefix">The configuration prefix for clients. Defaults to "ai:clients".</param>
    /// <param name="useDefaultProviders">Whether to register the default built-in <see cref="IChatClientProvider"/> providers for mapping configuration sections to <see cref="IChatClient"/> instances.</param>
    /// <returns>The host application builder.</returns>
    public static TBuilder AddChatClients<TBuilder>(this TBuilder builder, Action<string, ChatClientBuilder>? configurePipeline = default, Action<string, IChatClient>? configureClient = default, string prefix = "ai:clients", bool useDefaultProviders = true)
        where TBuilder : IHostApplicationBuilder
    {
        AddChatClients(builder.Services, builder.Configuration, configurePipeline, configureClient, prefix, useDefaultProviders);
        return builder;
    }

    /// <summary>
    /// Adds configuration-driven chat clients to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="configurePipeline">Optional action to configure the pipeline for each client.</param>
    /// <param name="configureClient">Optional action to configure each client.</param>
    /// <param name="prefix">The configuration prefix for clients. Defaults to "ai:clients".</param>
    /// <param name="useDefaultProviders">Whether to register the default built-in <see cref="IChatClientProvider"/> providers for mapping configuration sections to <see cref="IChatClient"/> instances.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddChatClients(this IServiceCollection services, IConfiguration configuration, Action<string, ChatClientBuilder>? configurePipeline = default, Action<string, IChatClient>? configureClient = default, string prefix = "ai:clients", bool useDefaultProviders = true)
    {
        // Ensure the factory and providers are registered
        services.AddChatClientFactory(useDefaultProviders);

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
                        sp.GetRequiredService<IChatClientFactory>(),
                        sp.GetRequiredService<ILogger<ConfigurableChatClient>>(),
                        section, id, configureClient);

                    if (configurePipeline != null)
                    {
                        var builder = client.AsBuilder();
                        configurePipeline(id, builder);
                        return builder.Build(sp);
                    }

                    return client;
                },
                options?.Lifetime ?? ServiceLifetime.Singleton));

            services.TryAdd(new ServiceDescriptor(typeof(IChatClient), new ServiceKey(id),
                factory: (sp, _) => sp.GetRequiredKeyedService<IChatClient>(id),
                options?.Lifetime ?? ServiceLifetime.Singleton));
        }

        return services;
    }

    /// <summary>Gets a chat client by id (case-insensitive) from the service provider.</summary>
    public static IChatClient? GetChatClient(this IServiceProvider services, string id)
        => services.GetKeyedService<IChatClient>(id) ?? services.GetKeyedService<IChatClient>(new ServiceKey(id));

    internal class ChatClientOptions : OpenAIClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;
    }
}
