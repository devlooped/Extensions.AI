using System.Text.Json;
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
public sealed partial class ConfigurableAIAgent : AIAgent, IDisposable
{
    readonly IServiceProvider services;
    readonly IConfiguration configuration;
    readonly string section;
    readonly string name;
    readonly ILogger logger;
    readonly Action<string, ChatClientAgentOptions>? configure;
    IDisposable reloadToken;
    ChatClientAgent agent;
    IChatClient chat;
    ChatClientAgentOptions options;

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

        (agent, options, chat) = Configure(configuration.GetRequiredSection(section));
        reloadToken = configuration.GetReloadToken().RegisterChangeCallback(OnReload, state: null);
    }

    /// <summary>Disposes the client and stops monitoring configuration changes.</summary>
    public void Dispose() => reloadToken?.Dispose();

    /// <inheritdoc/>
    public override object? GetService(Type serviceType, object? serviceKey = null) => serviceType switch
    {
        Type t when t == typeof(ChatClientAgentOptions) => options,
        Type t when t == typeof(IChatClient) => chat,
        _ => agent.GetService(serviceType, serviceKey)
    };

    /// <inheritdoc/>
    public override string Id => agent.Id;
    /// <inheritdoc/>
    public override string? Description => agent.Description;
    /// <inheritdoc/>
    public override string DisplayName => agent.DisplayName;
    /// <inheritdoc/>
    public override string? Name => this.name;
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

    (ChatClientAgent, ChatClientAgentOptions, IChatClient) Configure(IConfigurationSection configSection)
    {
        var options = configSection.Get<AgentClientOptions>();
        options?.Name ??= name;

        // If there was a custom id, we must validate it didn't change since that's not supported.
        if (configuration[$"{section}:name"] is { } newname && newname != name)
            throw new InvalidOperationException($"The name of a configured agent cannot be changed at runtime. Expected '{name}' but was '{newname}'.");

        var client = services.GetRequiredKeyedService<IChatClient>(options?.Client
            ?? throw new InvalidOperationException($"A client must be specified for agent '{name}' in configuration section '{section}'."));

        var chat = configSection.GetSection("options").Get<ChatOptions>();
        if (chat is not null)
            options.ChatOptions = chat;

        configure?.Invoke(name, options);

        if (options.AIContextProviderFactory is null)
        {
            var contextFactory = services.GetKeyedService<AIContextProviderFactory>(name) ??
                services.GetService<AIContextProviderFactory>();

            if (contextFactory is not null)
                options.AIContextProviderFactory = contextFactory.CreateProvider;
        }

        if (options.ChatMessageStoreFactory is null)
        {
            var storeFactory = services.GetKeyedService<ChatMessageStoreFactory>(name) ??
                services.GetService<ChatMessageStoreFactory>();

            if (storeFactory is not null)
                options.ChatMessageStoreFactory = storeFactory.CreateStore;
        }

        LogConfigured(name);

        return (new ChatClientAgent(client, options, services.GetRequiredService<ILoggerFactory>(), services), options, client);
    }

    void OnReload(object? state)
    {
        var configSection = configuration.GetRequiredSection(section);
        reloadToken?.Dispose();
        chat?.Dispose();
        (agent, options, chat) = Configure(configSection);
        reloadToken = configuration.GetReloadToken().RegisterChangeCallback(OnReload, state: null);
    }

    [LoggerMessage(LogLevel.Information, "AIAgent '{Id}' configured.")]
    private partial void LogConfigured(string id);

    class AgentClientOptions : ChatClientAgentOptions
    {
        public required string Client { get; set; }
    }
}
