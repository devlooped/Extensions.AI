using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Devlooped.Extensions.AI;

/// <summary>A configuration-driven <see cref="IChatClient"/> which monitors configuration changes and re-applies them to the inner client automatically.</summary>
sealed partial class ConfigurableChatClient : IChatClient, IDisposable
{
    readonly IClientFactory factory;
    readonly IConfiguration configuration;
    readonly string section;
    readonly string id;
    readonly ILogger logger;
    IDisposable reloadToken;
    IChatClient innerClient;
    ChatClientMetadata metadata;

    /// <summary>Initializes a new instance of the <see cref="ConfigurableChatClient"/> class using an explicit client factory.</summary>
    /// <param name="factory">The factory used to create (and re-create on reload) the inner chat client.</param>
    /// <param name="configuration">The configuration to read settings from (used for id validation and reload tokens).</param>
    /// <param name="logger">The logger to use for logging.</param>
    /// <param name="section">The configuration section path.</param>
    /// <param name="id">The unique identifier for the client.</param>
    internal ConfigurableChatClient(IClientFactory factory, IConfiguration configuration, ILogger logger, string section, string id)
    {
        if (section.Contains('.'))
            throw new ArgumentException("Section separator must be ':', not '.'");

        this.factory = Throw.IfNull(factory);
        this.configuration = Throw.IfNull(configuration);
        this.logger = Throw.IfNull(logger);
        this.section = Throw.IfNullOrEmpty(section);
        this.id = Throw.IfNullOrEmpty(id);

        (innerClient, metadata) = Configure();
        reloadToken = configuration.GetReloadToken().RegisterChangeCallback(OnReload, state: null);
    }

    /// <summary>Disposes the client and stops monitoring configuration changes.</summary>
    public void Dispose() => reloadToken?.Dispose();

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null) => serviceType switch
    {
        Type t when typeof(ChatClientMetadata).IsAssignableFrom(t) => metadata,
        Type t when t == typeof(IChatClient) => this,
        _ => innerClient.GetService(serviceType, serviceKey)
    };

    /// <inheritdoc/>
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => innerClient.GetResponseAsync(messages, options, cancellationToken);
    /// <inheritdoc/>
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => innerClient.GetStreamingResponseAsync(messages, options, cancellationToken);

    (IChatClient, ChatClientMetadata) Configure()
    {
        // If there was a custom id, we must validate it didn't change since that's not supported.
        if (configuration[$"{section}:id"] is { } newid && newid != id)
            throw new InvalidOperationException($"The ID of a configured client cannot be changed at runtime. Expected '{id}' but was '{newid}'.");

        var client = factory.CreateChatClient();
        LogConfigured(id);
        var clientMetadata = client.GetService<ChatClientMetadata>() ?? new ChatClientMetadata(null, null, null);

        return (client, new ConfigurableChatClientMetadata(id, section, clientMetadata.ProviderName, clientMetadata.ProviderUri, clientMetadata.DefaultModelId));
    }

    void OnReload(object? state)
    {
        (innerClient as IDisposable)?.Dispose();
        reloadToken?.Dispose();

        (innerClient, metadata) = Configure();

        reloadToken = configuration.GetReloadToken().RegisterChangeCallback(OnReload, state: null);
    }

    [LoggerMessage(LogLevel.Information, "ChatClient '{Id}' configured.")]
    private partial void LogConfigured(string id);
}

/// <summary>Metadata for a <see cref="ConfigurableChatClient"/>.</summary>
public class ConfigurableChatClientMetadata(string id, string configurationSection, string? providerName, Uri? providerUri, string? defaultModelId)
    : ChatClientMetadata(providerName, providerUri, defaultModelId)
{
    /// <summary>The unique identifier of the configurable client.</summary>
    public string Id => id;
    /// <summary>The configuration section used to configure the client.</summary>
    public string ConfigurationSection => configurationSection;
}
