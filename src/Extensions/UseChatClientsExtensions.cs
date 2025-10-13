using Devlooped.Extensions.AI.Grok;
using Devlooped.Extensions.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace Devlooped.Extensions.AI;

public static class UseChatClientsExtensions
{
    public static IServiceCollection UseChatClients(this IServiceCollection services, IConfiguration configuration, Action<string, ChatClientBuilder>? configurePipeline = default, Action<string, IChatClient>? configureClient = default, string prefix = "ai:clients")
    {
        foreach (var entry in configuration.AsEnumerable().Where(x =>
            x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            x.Key.EndsWith("modelid", StringComparison.OrdinalIgnoreCase)))
        {
            var section = string.Join(':', entry.Key.Split(':')[..^1]);
            // ID == section after clients:, with optional overridable id
            var id = configuration[$"{section}:id"] ?? section[(prefix.Length + 1)..];

            var options = configuration.GetRequiredSection(section).Get<ChatClientOptions>();
            // We need logging set up for the configurable client to log changes
            services.AddLogging();

            var builder = services.AddKeyedChatClient(id,
                services => new ConfigurableChatClient(configuration, services.GetRequiredService<ILogger<ConfigurableChatClient>>(), section, id, configureClient),
                options?.Lifetime ?? ServiceLifetime.Singleton);

            configurePipeline?.Invoke(id, builder);
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
