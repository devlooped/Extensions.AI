using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using Devlooped.Agents.AI;
using Devlooped.Extensions.AI;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using ModelContextProtocol.Server;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Adds configuration-driven agents to an application host.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConfigurableAgentsExtensions
{
    /// <summary>Adds <see cref="McpServerTool"/> instances to the service collection backing <paramref name="builder"/>.</summary>
    /// <typeparam name="TToolType">The tool type.</typeparam>
    /// <param name="builder">The builder instance.</param>
    /// <param name="serializerOptions">The serializer options governing tool parameter marshalling.</param>
    /// <returns>The builder provided in <paramref name="builder"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This method discovers all instance and static methods (public and non-public) on the specified <typeparamref name="TToolType"/>
    /// type, where the methods are attributed as <see cref="McpServerToolAttribute"/>, and adds an <see cref="AIFunction"/>
    /// instance for each. For instance methods, an instance will be constructed for each invocation of the tool.
    /// </remarks>
    public static IAIAgentsBuilder WithTools<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods |
        DynamicallyAccessedMemberTypes.PublicConstructors)] TToolType>(
        this IAIAgentsBuilder builder,
        JsonSerializerOptions? serializerOptions = null,
        ServiceLifetime lifetime = ServiceLifetime.Singleton)
    {
        Throw.IfNull(builder);

        // Preserve existing registration if any, such as when using Devlooped.Extensions.DependencyInjection
        // via [Service] attribute or by convention.
        builder.Services.TryAdd(ServiceDescriptor.Describe(typeof(TToolType), typeof(TToolType), lifetime));

        serializerOptions ??= ToolJsonOptions.Default;

        foreach (var toolMethod in typeof(TToolType).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        {
            if (toolMethod.GetCustomAttribute<McpServerToolAttribute>() is { } toolAttribute)
            {
                var function = toolMethod.IsStatic
                    ? AIFunctionFactory.Create(toolMethod, null,
                        toolAttribute.Name ?? ToolJsonOptions.Default.PropertyNamingPolicy!.ConvertName(toolMethod.Name),
                        serializerOptions: serializerOptions)
                    : AIFunctionFactory.Create(toolMethod, args => args.Services?.GetRequiredService(typeof(TToolType)) ??
                        throw new InvalidOperationException("Could not determine target instance for tool."),
                        new AIFunctionFactoryOptions
                        {
                            Name = toolAttribute.Name ?? ToolJsonOptions.Default.PropertyNamingPolicy!.ConvertName(toolMethod.Name),
                            SerializerOptions = serializerOptions
                        });

                builder.Services.TryAdd(ServiceDescriptor.DescribeKeyed(
                    typeof(AIFunction), function.Name,
                    (_, _) => function, lifetime));
            }
        }

        return builder;
    }

    /// <summary>
    /// Adds AI agents to the host application builder based on configuration.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configurePipeline">Optional action to configure the pipeline for each agent.</param>
    /// <param name="configureOptions">Optional action to configure options for each agent.</param>
    /// <param name="prefix">The configuration prefix for agents, defaults to "ai:agents".</param>
    /// <returns>The host application builder with AI agents added.</returns>
    public static IAIAgentsBuilder AddAIAgents(this IHostApplicationBuilder builder, Action<string, AIAgentBuilder>? configurePipeline = default, Action<string, ChatClientAgentOptions>? configureOptions = default, string prefix = "ai:agents")
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

            // Also register for case-insensitive lookup, but without duplicating the entry in 
            // the AgentCatalog, since that will always resolve from above.
            builder.Services.TryAdd(ServiceDescriptor.KeyedSingleton(new ServiceKey(name), (sp, key)
                => sp.GetRequiredKeyedService<AIAgent>(name)));
        }

        return new DefaultAIAgentsBuilder(builder);
    }

    /// <summary>Gets an AI agent by name (case-insensitive) from the service provider.</summary>
    public static AIAgent? GetIAAgent(this IServiceProvider services, string name)
        => services.GetKeyedService<AIAgent>(name) ?? services.GetKeyedService<AIAgent>(new ServiceKey(name));
}