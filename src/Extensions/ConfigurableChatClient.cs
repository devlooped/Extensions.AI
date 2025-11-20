using System.ClientModel.Primitives;
using System.ComponentModel;
using Azure;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Devlooped.Extensions.AI.Grok;
using Devlooped.Extensions.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI;

namespace Devlooped.Extensions.AI;

/// <summary>
/// A configuration-driven <see cref="IChatClient"/> which monitors configuration changes and 
/// re-applies them to the inner client automatically.
/// </summary>
public sealed partial class ConfigurableChatClient : IChatClient, IDisposable
{
    readonly IConfiguration configuration;
    readonly string section;
    readonly string id;
    readonly ILogger logger;
    readonly Action<string, IChatClient>? configure;
    IDisposable reloadToken;
    IChatClient innerClient;
    ChatClientMetadata metadata;
    object? options;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurableChatClient"/> class.
    /// </summary>
    /// <param name="configuration">The configuration to read settings from.</param>
    /// <param name="logger">The logger to use for logging.</param>
    /// <param name="section">The configuration section to use.</param>
    /// <param name="id">The unique identifier for the client.</param>
    /// <param name="configure">An optional action to configure the client after creation.</param>
    public ConfigurableChatClient(IConfiguration configuration, ILogger logger, string section, string id, Action<string, IChatClient>? configure)
    {
        if (section.Contains('.'))
            throw new ArgumentException("Section separator must be ':', not '.'");

        this.configuration = Throw.IfNull(configuration);
        this.logger = Throw.IfNull(logger);
        this.section = Throw.IfNullOrEmpty(section);
        this.id = Throw.IfNullOrEmpty(id);
        this.configure = configure;

        (innerClient, metadata) = Configure(configuration.GetRequiredSection(section));
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

    /// <summary>Exposes the optional <see cref="ClientPipelineOptions"/> configured for the client.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public object? Options => options;

    (IChatClient, ChatClientMetadata) Configure(IConfigurationSection configSection)
    {
        var options = SetOptions<ConfigurableClientOptions>(configSection);
        Throw.IfNullOrEmpty(options?.ModelId, $"{configSection}:modelid");

        // If there was a custom id, we must validate it didn't change since that's not supported.
        if (configuration[$"{section}:id"] is { } newid && newid != id)
            throw new InvalidOperationException($"The ID of a configured client cannot be changed at runtime. Expected '{id}' but was '{newid}'.");

        var apikey = options!.ApiKey;
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

        Throw.IfNullOrEmpty(apikey, $"{section}:apikey");

        IChatClient client = options.Endpoint?.Host == "api.x.ai"
            ? new GrokChatClient2(apikey, options.ModelId, options)
            : options.Endpoint?.Host == "ai.azure.com"
            ? new ChatCompletionsClient(options.Endpoint, new AzureKeyCredential(apikey), SetOptions<ConfigurableInferenceOptions>(configSection)).AsIChatClient(options.ModelId)
            : options.Endpoint?.Host.EndsWith("openai.azure.com") == true
            ? new AzureOpenAIChatClient(options.Endpoint, new AzureKeyCredential(apikey), options.ModelId, SetOptions<ConfigurableAzureOptions>(configSection))
            : new OpenAIChatClient(apikey, options.ModelId, options);

        configure?.Invoke(id, client);

        LogConfigured(id);

        var metadata = client.GetService<ChatClientMetadata>() ?? new ChatClientMetadata(null, null, null);

        return (client, new ConfigurableChatClientMetadata(id, section, metadata.ProviderName, metadata.ProviderUri, metadata.DefaultModelId));
    }

    TOptions? SetOptions<TOptions>(IConfigurationSection section) where TOptions : class
    {
        var options = typeof(TOptions) switch
        {
            var t when t == typeof(ConfigurableClientOptions) => section.Get<ConfigurableClientOptions>() as TOptions,
            var t when t == typeof(ConfigurableInferenceOptions) => section.Get<ConfigurableInferenceOptions>() as TOptions,
            var t when t == typeof(ConfigurableAzureOptions) => section.Get<ConfigurableAzureOptions>() as TOptions,
#pragma warning disable SYSLIB1104 // The target type for a binder call could not be determined
            _ => section.Get<TOptions>()
#pragma warning restore SYSLIB1104 // The target type for a binder call could not be determined
        };

        this.options = options;
        return options;
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

    internal class ConfigurableClientOptions : OpenAIClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
    }

    internal class ConfigurableInferenceOptions : AzureAIInferenceClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
    }

    internal class ConfigurableAzureOptions : AzureOpenAIClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
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