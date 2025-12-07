using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Devlooped.Extensions.AI;
using Devlooped.Extensions.AI.Grok;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Devlooped.Agents.AI;

/// <summary>
/// A configuration-driven <see cref="AIAgent"/> which monitors configuration changes and 
/// re-applies them to the inner agent automatically.
/// </summary>
public sealed partial class ConfigurableAIAgent : AIAgent, IHasAdditionalProperties, IDisposable
{
    readonly IServiceProvider services;
    readonly IConfiguration configuration;
    readonly string section;
    readonly string name;
    readonly ILogger logger;
    readonly Action<string, ChatClientAgentOptions>? configure;
    IDisposable reloadToken;
    ChatClientAgent agent;
    ChatClientAgentOptions options;
    IChatClient chat;
    AIAgentMetadata metadata;

    public ConfigurableAIAgent(IServiceProvider services, string section, string name, Action<string, ChatClientAgentOptions>? configure)
    {
        if (section.Contains('.'))
            throw new ArgumentException("Section separator must be ':', not '.'");

        this.services = Throw.IfNull(services);
        this.configuration = services.GetRequiredService<IConfiguration>();
        this.logger = services.GetRequiredService<ILogger<ConfigurableAIAgent>>();
        this.section = Throw.IfNullOrEmpty(section);
        this.name = Throw.IfNullOrEmpty(name);
        this.configure = configure;

        (agent, options, chat, metadata) = Configure(configuration.GetRequiredSection(section));
        reloadToken = configuration.GetReloadToken().RegisterChangeCallback(OnReload, state: null);
    }

    /// <summary>Disposes the client and stops monitoring configuration changes.</summary>
    public void Dispose() => reloadToken?.Dispose();

    /// <inheritdoc/>
    public override object? GetService(Type serviceType, object? serviceKey = null) => serviceType switch
    {
        Type t when t == typeof(ChatClientAgentOptions) => options,
        Type t when t == typeof(IChatClient) => chat,
        Type t when typeof(AIAgentMetadata).IsAssignableFrom(t) => metadata,
        _ => agent.GetService(serviceType, serviceKey)
    };

    /// <inheritdoc/>
    public AdditionalPropertiesDictionary? AdditionalProperties { get; set; }

    /// <inheritdoc/>
    public override string Id => agent.Id;
    /// <inheritdoc/>
    public override string? Description => agent.Description;
    /// <inheritdoc/>
    public override string DisplayName => agent.DisplayName;
    /// <inheritdoc/>
    public override string? Name => name;
    /// <inheritdoc/>
    public override AgentThread DeserializeThread(JsonElement serializedThread, JsonSerializerOptions? jsonSerializerOptions = null)
        => agent.DeserializeThread(serializedThread, jsonSerializerOptions);
    /// <inheritdoc/>
    public override AgentThread GetNewThread() => agent.GetNewThread();
    /// <inheritdoc/>
    public override Task<AgentRunResponse> RunAsync(IEnumerable<ChatMessage> messages, AgentThread? thread = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
        => agent.RunAsync(messages, thread, options, cancellationToken);
    /// <inheritdoc/>
    public override IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingAsync(IEnumerable<ChatMessage> messages, AgentThread? thread = null, AgentRunOptions? options = null, CancellationToken cancellationToken = default)
        => agent.RunStreamingAsync(messages, thread, options, cancellationToken);

    /// <summary>
    /// Configured agent options.
    /// </summary>
    public ChatClientAgentOptions Options => options;

    (ChatClientAgent, ChatClientAgentOptions, IChatClient, AIAgentMetadata) Configure(IConfigurationSection configSection)
    {
        var options = configSection.Get<AgentClientOptions>();
        options?.Name ??= name;
        options?.Description = options?.Description?.Dedent();

        var properties = configSection.Get<AdditionalPropertiesDictionary>();
        if (properties is not null)
        {
            properties.Remove(nameof(AgentClientOptions.Name));
            properties.Remove(nameof(AgentClientOptions.Description));
            properties.Remove(nameof(AgentClientOptions.Client));
            properties.Remove(nameof(AgentClientOptions.Use));
            properties.Remove(nameof(AgentClientOptions.Tools));
            properties.Remove(nameof(ChatOptions.ModelId));
            properties.Remove(nameof(ChatOptions.Instructions));

            AdditionalProperties = properties.Count == 0 ? null : properties;
        }

        // If there was a custom id, we must validate it didn't change since that's not supported.
        if (configuration[$"{section}:name"] is { } newname && newname != name)
            throw new InvalidOperationException($"The name of a configured agent cannot be changed at runtime. Expected '{name}' but was '{newname}'.");

        var client = services.GetKeyedService<IChatClient>(options?.Client
            ?? throw new InvalidOperationException($"A client must be specified for agent '{name}' in configuration section '{section}'."))
            ?? services.GetKeyedService<IChatClient>(new ServiceKey(options!.Client))
            ?? throw new InvalidOperationException($"Specified chat client '{options!.Client}' for agent '{name}' is not registered.");

        var provider = client.GetService<ChatClientMetadata>()?.ProviderName;
        ChatOptions? chat = provider == "xai"
            ? configSection.GetSection("options").Get<GrokChatOptions>()
            : configSection.GetSection("options").Get<ExtendedChatOptions>();

        chat?.Instructions = chat?.Instructions?.Dedent();

        if (chat is not null)
            options.ChatOptions = chat;

        configure?.Invoke(name, options);

        if (options.AIContextProviderFactory is null)
        {
            var contextFactory = services.GetKeyedService<AIContextProviderFactory>(name) ??
                services.GetService<AIContextProviderFactory>();

            if (contextFactory is not null)
            {
                if (options.Use?.Count > 0 || options.Tools?.Count > 0)
                    throw new InvalidOperationException($"Invalid simultaneous use of keyed service {nameof(AIContextProviderFactory)} and '{section}:use/tools' in configuration.");

                options.AIContextProviderFactory = contextFactory.CreateProvider;
            }
            else
            {
                var contexts = new List<AIContextProvider>();
                if (services.GetKeyedService<AIContextProvider>(name) is { } contextProvider)
                    contexts.Add(contextProvider);

                foreach (var use in options.Use ?? [])
                {
                    if (services.GetKeyedService<AIContext>(use) is { } staticContext)
                    {
                        contexts.Add(new StaticAIContextProvider(staticContext));
                        continue;
                    }
                    else if (services.GetKeyedService<AIContextProvider>(use) is { } dynamicContext)
                    {
                        contexts.Add(dynamicContext);
                        continue;
                    }

                    // Else, look for a config section.
                    if (configuration.GetSection("ai:context:" + use) is { } ctxSection &&
                        ctxSection.Get<AIContextConfiguration>() is { } ctxConfig)
                    {
                        var configured = new AIContext();
                        if (ctxConfig.Instructions is not null)
                            configured.Instructions = ctxConfig.Instructions.Dedent();
                        if (ctxConfig.Messages is { Count: > 1 } messages)
                            configured.Messages = messages;

                        if (ctxConfig.Tools is not null)
                        {
                            foreach (var toolName in ctxConfig.Tools)
                            {
                                var tool = services.GetKeyedService<AITool>(toolName) ??
                                    services.GetKeyedService<AIFunction>(toolName) ??
                                    throw new InvalidOperationException($"Specified tool '{toolName}' for AI context '{ctxSection.Path}:tools' is not registered as a keyed {nameof(AITool)} or {nameof(AIFunction)}, and is required by agent section '{configSection.Path}'.");

                                configured.Tools ??= [];
                                configured.Tools.Add(tool);
                            }
                        }

                        contexts.Add(new StaticAIContextProvider(configured));
                        continue;
                    }

                    throw new InvalidOperationException($"Specified AI context '{use}' for agent '{name}' is not registered as either {nameof(AIContent)} or configuration section 'ai:context:{use}'.");
                }

                foreach (var toolName in options.Tools ?? [])
                {
                    var tool = services.GetKeyedService<AITool>(toolName) ??
                        services.GetKeyedService<AIFunction>(toolName) ??
                        throw new InvalidOperationException($"Specified tool '{toolName}' for agent '{section}' is not registered as a keyed {nameof(AITool)}, {nameof(AIFunction)} or MCP server tools.");

                    contexts.Add(new StaticAIContextProvider(new AIContext { Tools = [tool] }));
                }

                options.AIContextProviderFactory = _ => new CompositeAIContextProvider(contexts);
            }
        }
        else if (options.Use?.Count > 0)
        {
            throw new InvalidOperationException($"Invalid simultaneous use of {nameof(ChatClientAgentOptions)}.{nameof(ChatClientAgentOptions.AIContextProviderFactory)} and '{section}:use' in configuration.");
        }

        if (options.ChatMessageStoreFactory is null)
        {
            var storeFactory = services.GetKeyedService<ChatMessageStoreFactory>(name) ??
                services.GetService<ChatMessageStoreFactory>();

            if (storeFactory is not null)
                options.ChatMessageStoreFactory = storeFactory.CreateStore;
        }

        LogConfigured(name);

        var agent = new ChatClientAgent(client, options, services.GetRequiredService<ILoggerFactory>(), services);
        var metadata = agent.GetService<AIAgentMetadata>() ?? new AIAgentMetadata(provider);

        return (agent, options, client, new ConfigurableAIAgentMetadata(name, section, metadata.ProviderName));
    }

    void OnReload(object? state)
    {
        var configSection = configuration.GetRequiredSection(section);
        reloadToken?.Dispose();
        chat?.Dispose();
        (agent, options, chat, metadata) = Configure(configSection);
        reloadToken = configuration.GetReloadToken().RegisterChangeCallback(OnReload, state: null);
    }

    [LoggerMessage(LogLevel.Information, "AIAgent '{Id}' configured.")]
    private partial void LogConfigured(string id);

    internal class AgentClientOptions : ChatClientAgentOptions
    {
        public string? Client { get; set; }
        public IList<string>? Use { get; set; }
        public IList<string>? Tools { get; set; }
    }
}

/// <summary>Metadata for a <see cref="ConfigurableAIAgent"/>.</summary>

[DebuggerDisplay("Name = {Name}, Section = {ConfigurationSection}, ProviderName = {ProviderName}")]
public class ConfigurableAIAgentMetadata(string name, string configurationSection, string? providerName) : AIAgentMetadata(providerName)
{
    /// <summary>Name of the agent.</summary>
    public string Name => name;
    /// <summary>Configuration section where the agent is defined.</summary>
    public string ConfigurationSection = configurationSection;
}

class AIContextConfiguration
{
    public string? Instructions { get; set; }

    public IList<ChatMessage>? Messages =>
        MessageConfigurations?.Select(config =>
            config.System is not null ? new ChatMessage(ChatRole.System, config.System) :
            config.User is not null ? new ChatMessage(ChatRole.User, config.User) :
            config.Assistant is not null ? new ChatMessage(ChatRole.Assistant, config.Assistant) :
            null).Where(x => x is not null).Cast<ChatMessage>().ToList();

    public IList<string>? Tools { get; set; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [ConfigurationKeyName("Messages")]
    public MessageConfiguration[]? MessageConfigurations { get; set; }
}

record MessageConfiguration(string? System = default, string? User = default, string? Assistant = default);
