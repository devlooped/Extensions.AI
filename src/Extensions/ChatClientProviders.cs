using System.ClientModel;
using Azure;
using Azure.AI.Inference;
using Azure.AI.OpenAI;
using Devlooped.Extensions.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using xAI;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Provides <see cref="IChatClient"/> instances for the OpenAI API.
/// </summary>
sealed class OpenAIChatClientProvider : IChatClientProvider
{
    /// <inheritdoc/>
    public string ProviderName => "openai";

    /// <inheritdoc/>
    public Uri? BaseUri => new("https://api.openai.com/");

    /// <inheritdoc/>
    public string? HostSuffix => null;

    /// <inheritdoc/>
    public IChatClient Create(IConfigurationSection section)
    {
        var options = section.Get<OpenAIProviderOptions>() ?? new();
        Throw.IfNullOrEmpty(options.ApiKey, $"{section.Path}:apikey");
        Throw.IfNullOrEmpty(options.ModelId, $"{section.Path}:modelid");

        return new ProviderOptionsChatClient<OpenAIClientOptions>(
            new OpenAIClient(new ApiKeyCredential(options.ApiKey), options).GetChatClient(options.ModelId).AsIChatClient(),
            options);
    }

    internal sealed class OpenAIProviderOptions : OpenAIClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
    }
}

/// <summary>
/// Provides <see cref="IChatClient"/> instances for the Azure OpenAI API.
/// </summary>
sealed class AzureOpenAIChatClientProvider : IChatClientProvider
{
    /// <inheritdoc/>
    public string ProviderName => "azure.openai";

    /// <inheritdoc/>
    public Uri? BaseUri => null;

    /// <inheritdoc/>
    public string? HostSuffix => ".openai.azure.com";

    /// <inheritdoc/>
    public IChatClient Create(IConfigurationSection section)
    {
        var options = section.Get<AzureOpenAIProviderOptions>() ?? new();
        Throw.IfNullOrEmpty(options.ApiKey, $"{section.Path}:apikey");
        Throw.IfNullOrEmpty(options.ModelId, $"{section.Path}:modelid");
        Throw.IfNull(options.Endpoint, $"{section.Path}:endpoint");

        return new ProviderOptionsChatClient<AzureOpenAIClientOptions>(
            new AzureOpenAIChatClient(options.Endpoint, new AzureKeyCredential(options.ApiKey), options.ModelId, options),
            options);
    }

    internal sealed class AzureOpenAIProviderOptions : Azure.AI.OpenAI.AzureOpenAIClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
        public Uri? Endpoint { get; set; }
    }
}

/// <summary>
/// Provides <see cref="IChatClient"/> instances for the Azure AI Inference API.
/// </summary>
sealed class AzureAIInferenceChatClientProvider : IChatClientProvider
{
    /// <inheritdoc/>
    public string ProviderName => "azure.inference";

    /// <inheritdoc/>
    public Uri? BaseUri => new("https://ai.azure.com/");

    /// <inheritdoc/>
    public string? HostSuffix => null;

    /// <inheritdoc/>
    public IChatClient Create(IConfigurationSection section)
    {
        var options = section.Get<AzureInferenceProviderOptions>() ?? new();
        Throw.IfNullOrEmpty(options.ApiKey, $"{section.Path}:apikey");
        Throw.IfNullOrEmpty(options.ModelId, $"{section.Path}:modelid");
        Throw.IfNull(options.Endpoint, $"{section.Path}:endpoint");

        return new ProviderOptionsChatClient<AzureAIInferenceClientOptions>(
            new Azure.AI.Inference.ChatCompletionsClient(options.Endpoint, new AzureKeyCredential(options.ApiKey), options)
                .AsIChatClient(options.ModelId),
            options);
    }

    internal sealed class AzureInferenceProviderOptions : Azure.AI.Inference.AzureAIInferenceClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
        public Uri? Endpoint { get; set; }
    }
}

/// <summary>
/// Provides <see cref="IChatClient"/> instances for the Grok (xAI) API.
/// </summary>
sealed class GrokChatClientProvider : IChatClientProvider
{
    /// <inheritdoc/>
    public string ProviderName => "xai";

    /// <inheritdoc/>
    public Uri? BaseUri => new("https://api.x.ai/");

    /// <inheritdoc/>
    public string? HostSuffix => null;

    /// <inheritdoc/>
    public IChatClient Create(IConfigurationSection section)
    {
        var options = section.Get<GrokProviderOptions>() ?? new();
        Throw.IfNullOrEmpty(options.ApiKey, $"{section.Path}:apikey");
        Throw.IfNullOrEmpty(options.ModelId, $"{section.Path}:modelid");

        return new ProviderOptionsChatClient<GrokClientOptions>(
            new GrokClient(options.ApiKey, options).AsIChatClient(options.ModelId),
            options);
    }

    internal sealed class GrokProviderOptions : GrokClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
    }
}

sealed class ProviderOptionsChatClient<TOptions>(IChatClient inner, TOptions options) : DelegatingChatClient(inner)
    where TOptions : notnull
{
    public override object? GetService(Type serviceType, object? serviceKey = null)
        => IsOptionsRequest(serviceType, serviceKey) ? options : inner.GetService(serviceType, serviceKey);

    bool IsOptionsRequest(Type serviceType, object? serviceKey)
        => serviceType == typeof(object) ?
           serviceKey is string key && string.Equals(key, "options", StringComparison.OrdinalIgnoreCase) :
           typeof(TOptions).IsAssignableFrom(serviceType);
}
