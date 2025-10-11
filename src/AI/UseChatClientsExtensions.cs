using Devlooped.Extensions.AI.Grok;
using Devlooped.Extensions.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;

namespace Devlooped.Extensions.AI;

public static class UseChatClientsExtensions
{
    public static IServiceCollection UseChatClients(this IServiceCollection services, IConfiguration configuration, Action<string, ChatClientBuilder>? configure = default, string prefix = "ai:clients")
    {
        foreach (var entry in configuration.AsEnumerable().Where(x =>
            x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            x.Key.EndsWith("modelid", StringComparison.OrdinalIgnoreCase)))
        {
            var section = string.Join(':', entry.Key.Split(':')[..^1]);
            // ID == section after clients:, with optional overridable id
            var id = configuration[$"{section}:id"] ?? section[(prefix.Length + 1)..];
                
            var options = configuration.GetSection(section).Get<ChatClientOptions>();
            Throw.IfNullOrEmpty(options?.ModelId, entry.Key);

            var apikey = options!.ApiKey;
            // If the key contains a section-like value, get it from config
            if (apikey?.Contains('.') == true || apikey?.Contains(':') == true)
                apikey = configuration[apikey.Replace('.', ':')] ?? configuration[apikey.Replace('.', ':') + ":apikey"];

            var keysection = section;
            // ApiKey inheritance by section parents. 
            // i.e. section ai:clients:grok:router does not need to have its own key, 
            // it will inherit from ai:clients:grok:key, for example.
            while (string.IsNullOrEmpty(apikey))
            {
                keysection = string.Join(':', keysection.Split(':')[..^1]);
                if (string.IsNullOrEmpty(keysection))
                    break;
                apikey = configuration[$"{keysection}:apikey"];
            }

            Throw.IfNullOrEmpty(apikey, $"{section}:apikey");

            var builder = services.AddKeyedChatClient(id, services =>
            {
                if (options.Endpoint?.Host == "api.x.ai")
                    return new GrokChatClient(apikey, options.ModelId, options);

                return new OpenAIChatClient(apikey, options.ModelId, options);
            }, options.Lifetime);

            configure?.Invoke(id, builder);
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
