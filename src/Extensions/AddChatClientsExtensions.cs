using System.ComponentModel;
using Devlooped.Extensions.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Adds configuration-driven chat clients to an application host or service collection.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AddChatClientsExtensions
{
    /// <summary>
    /// Adds configuration-driven chat clients to the host application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configurePipeline">Optional action to configure the pipeline for each client.</param>
    /// <param name="configureClient">Optional action to configure each client.</param>
    /// <param name="prefix">The configuration prefix for clients. Defaults to "ai:clients".</param>
    /// <returns>The host application builder.</returns>
    public static IHostApplicationBuilder AddChatClients(this IHostApplicationBuilder builder, Action<string, ChatClientBuilder>? configurePipeline = default, Action<string, IChatClient>? configureClient = default, string prefix = "ai:clients")
    {
        AddChatClients(builder.Services, builder.Configuration, configurePipeline, configureClient, prefix);
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
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddChatClients(this IServiceCollection services, IConfiguration configuration, Action<string, ChatClientBuilder>? configurePipeline = default, Action<string, IChatClient>? configureClient = default, string prefix = "ai:clients")
    {
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
                    var client = new ConfigurableChatClient(configuration, sp.GetRequiredService<ILogger<ConfigurableChatClient>>(), section, id, configureClient);

                    if (configurePipeline != null)
                    {
                        var builder = client.AsBuilder();
                        configurePipeline(id, builder);
                        return builder.Build(sp);
                    }

                    return client;
                },
                options?.Lifetime ?? ServiceLifetime.Singleton));
        }

        return services;
    }

    internal class ChatClientOptions : OpenAIClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;
    }
}
