using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Devlooped.Extensions.AI;

/// <summary>
/// A configuration-driven <see cref="IChatClient"/> which monitors configuration changes and 
/// re-applies them to the inner client automatically.
/// </summary>
public sealed partial class ConfigurableChatClient : IChatClient, IDisposable
{
    readonly IConfiguration configuration;
    readonly IChatClientFactory factory;
    readonly string section;
    readonly string id;
    readonly ILogger logger;
    readonly Action<string, IChatClient>? configure;
    IDisposable reloadToken;
    IChatClient innerClient;
    ChatClientMetadata metadata;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurableChatClient"/> class.
    /// </summary>
    /// <param name="configuration">The configuration to read settings from.</param>
    /// <param name="factory">The factory to use for creating chat clients.</param>
    /// <param name="logger">The logger to use for logging.</param>
    /// <param name="section">The configuration section to use.</param>
    /// <param name="id">The unique identifier for the client.</param>
    /// <param name="configure">An optional action to configure the client after creation.</param>
    public ConfigurableChatClient(IConfiguration configuration, IChatClientFactory factory, ILogger logger, string section, string id, Action<string, IChatClient>? configure)
    {
        if (section.Contains('.'))
            throw new ArgumentException("Section separator must be ':', not '.'");

        this.configuration = Throw.IfNull(configuration);
        this.factory = Throw.IfNull(factory);
        this.logger = Throw.IfNull(logger);
        this.section = Throw.IfNullOrEmpty(section);
        this.id = Throw.IfNullOrEmpty(id);
        this.configure = configure;

        (innerClient, metadata) = Configure(configuration.GetRequiredSection(section));
        reloadToken = configuration.GetReloadToken().RegisterChangeCallback(OnReload, state: null);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurableChatClient"/> class 
    /// using the default <see cref="ChatClientFactory"/>.
    /// </summary>
    /// <param name="configuration">The configuration to read settings from.</param>
    /// <param name="logger">The logger to use for logging.</param>
    /// <param name="section">The configuration section to use.</param>
    /// <param name="id">The unique identifier for the client.</param>
    /// <param name="configure">An optional action to configure the client after creation.</param>
    public ConfigurableChatClient(IConfiguration configuration, ILogger logger, string section, string id, Action<string, IChatClient>? configure)
        : this(configuration, ChatClientFactory.CreateDefault(), logger, section, id, configure)
    {
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

    (IChatClient, ChatClientMetadata) Configure(IConfigurationSection configSection)
    {
        // If there was a custom id, we must validate it didn't change since that's not supported.
        if (configuration[$"{section}:id"] is { } newid && newid != id)
            throw new InvalidOperationException($"The ID of a configured client cannot be changed at runtime. Expected '{id}' but was '{newid}'.");

        // Resolve apikey from configuration with inheritance support
        var apikey = configSection["apikey"];
        // If the key contains a section-like value, get it from config
        if (apikey?.Contains('.') == true || apikey?.Contains(':') == true)
            apikey = configuration[apikey.Replace('.', ':')] ?? configuration[apikey.Replace('.', ':') + ":apikey"];

        var keysection = section;
        // ApiKey inheritance by section parents. 
        // i.e. section ai:clients:grok:router does not need to have its own key, 
        // it will inherit from ai:clients:grok:apikey, for example.
        while (string.IsNullOrEmpty(apikey))
        {
            keysection = string.Join(':', keysection.Split(':')[..^1]);
            if (string.IsNullOrEmpty(keysection))
                break;
            apikey = configuration[$"{keysection}:apikey"];
        }

        // Create a configuration section wrapper that includes the resolved apikey
        var effectiveSection = new ApiKeyResolvingConfigurationSection(configSection, apikey);

        IChatClient client = factory.CreateClient(effectiveSection);

        configure?.Invoke(id, client);

        LogConfigured(id);

        var metadata = client.GetService<ChatClientMetadata>() ?? new ChatClientMetadata(null, null, null);

        return (client, new ConfigurableChatClientMetadata(id, section, metadata.ProviderName, metadata.ProviderUri, metadata.DefaultModelId));
    }

    void OnReload(object? state)
    {
        var configSection = configuration.GetRequiredSection(section);

        (innerClient as IDisposable)?.Dispose();
        reloadToken?.Dispose();

        (innerClient, metadata) = Configure(configSection);

        reloadToken = configuration.GetReloadToken().RegisterChangeCallback(OnReload, state: null);
    }

    [LoggerMessage(LogLevel.Information, "ChatClient '{Id}' configured.")]
    private partial void LogConfigured(string id);

    /// <summary>
    /// A configuration section wrapper that overrides the apikey value with a resolved value.
    /// </summary>
    sealed class ApiKeyResolvingConfigurationSection(IConfigurationSection inner, string? resolvedApiKey) : IConfigurationSection
    {
        public string? this[string key]
        {
            get => string.Equals(key, "apikey", StringComparison.OrdinalIgnoreCase) && resolvedApiKey != null
                ? resolvedApiKey
                : inner[key];
            set => inner[key] = value;
        }

        public string Key => inner.Key;
        public string Path => inner.Path;
        public string? Value { get => inner.Value; set => inner.Value = value; }
        public IEnumerable<IConfigurationSection> GetChildren() => inner.GetChildren();
        public IChangeToken GetReloadToken() => inner.GetReloadToken();
        public IConfigurationSection GetSection(string key) => inner.GetSection(key);
    }
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