using Devlooped.Extensions.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace Devlooped.Extensions.AI;

public static class AddChatClientsExtensions
{
    public static IHostApplicationBuilder AddChatClients(this IHostApplicationBuilder builder, Action<string, ChatClientBuilder>? configurePipeline = default, Action<string, IChatClient>? configureClient = default, string prefix = "ai:clients")
    {
        AddChatClients(builder.Services, builder.Configuration, configurePipeline, configureClient, prefix);
        return builder;
    }

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

    class ChatClientOptions : OpenAIClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;
    }
}
