using Devlooped.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Devlooped.Agents.AI;

public static class AddAIAgentsExtensions
{
    public static IHostApplicationBuilder AddAIAgents(this IHostApplicationBuilder builder, Action<string, AIAgentBuilder>? configurePipeline = default, Action<string, ChatClientAgentOptions>? configureOptions = default, string prefix = "ai:agents")
    {
        builder.AddChatClients();

        foreach (var entry in builder.Configuration.AsEnumerable().Where(x =>
            x.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            x.Key.EndsWith("client", StringComparison.OrdinalIgnoreCase)))
        {
            var section = string.Join(':', entry.Key.Split(':')[..^1]);
            // key == name (unlike chat clients, the AddAIAgent expects the key to be the name). 
            var name = builder.Configuration[$"{section}:name"] ?? section[(prefix.Length + 1)..];

            var options = builder.Configuration.GetRequiredSection(section).Get<ChatClientAgentOptions>();
            // We need logging set up for the configurable client to log changes
            builder.Services.AddLogging();

            builder.AddAIAgent(name, (sp, key) =>
            {
                var agent = new ConfigurableAIAgent(sp, section, key, configureOptions);

                if (configurePipeline is not null)
                {
                    var builder = agent.AsBuilder();
                    configurePipeline(key, builder);
                    return builder.Build(sp);
                }

                return agent;
            });
        }

        return builder;
    }
}