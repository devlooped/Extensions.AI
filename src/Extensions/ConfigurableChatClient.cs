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

public sealed partial class ConfigurableChatClient : IDisposable, IChatClient
{
    readonly IConfiguration configuration;
    readonly string section;
    readonly string id;
    readonly ILogger logger;
    readonly Action<string, IChatClient>? configure;
    IDisposable reloadToken;
    IChatClient innerClient;

    public ConfigurableChatClient(IConfiguration configuration, ILogger logger, string section, string id, Action<string, IChatClient>? configure)
    {
        if (section.Contains('.'))
            throw new ArgumentException("Section separator must be ':', not '.'");

        this.configuration = Throw.IfNull(configuration);
        this.logger = Throw.IfNull(logger);
        this.section = Throw.IfNullOrEmpty(section);
        this.id = Throw.IfNullOrEmpty(id);
        this.configure = configure;

        innerClient = Configure(configuration.GetRequiredSection(section));
        reloadToken = configuration.GetReloadToken().RegisterChangeCallback(OnReload, state: null);
    }

    public void Dispose() => reloadToken?.Dispose();

    /// <inheritdoc/>
    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => innerClient.GetResponseAsync(messages, options, cancellationToken);
    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null)
        => innerClient.GetService(serviceType, serviceKey);
    /// <inheritdoc/>
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => innerClient.GetStreamingResponseAsync(messages, options, cancellationToken);

    IChatClient Configure(IConfigurationSection configSection)
    {
        var options = configSection.Get<ConfigurableClientOptions>();
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
            ? new GrokChatClient(apikey, options.ModelId, options)
            : options.Endpoint?.Host == "ai.azure.com"
            ? new ChatCompletionsClient(options.Endpoint, new AzureKeyCredential(apikey), configSection.Get<ConfigurableInferenceOptions>()).AsIChatClient(options.ModelId)
            : options.Endpoint?.Host.EndsWith("openai.azure.com") == true
            ? new AzureOpenAIChatClient(options.Endpoint, new AzureKeyCredential(apikey), options.ModelId, configSection.Get<ConfigurableAzureOptions>())
            : new OpenAIChatClient(apikey, options.ModelId, options);

        configure?.Invoke(id, client);

        LogConfigured(id);

        return client;
    }

    void OnReload(object? state)
    {
        var configSection = configuration.GetRequiredSection(section);

        (innerClient as IDisposable)?.Dispose();
        reloadToken?.Dispose();

        innerClient = Configure(configSection);

        reloadToken = configuration.GetReloadToken().RegisterChangeCallback(OnReload, state: null);
    }

    [LoggerMessage(LogLevel.Information, "ChatClient {Id} configured.")]
    private partial void LogConfigured(string id);

    class ConfigurableClientOptions : OpenAIClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
    }

    class ConfigurableInferenceOptions : AzureAIInferenceClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
    }

    class ConfigurableAzureOptions : AzureOpenAIClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
    }
}
