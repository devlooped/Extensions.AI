using Azure;
using Devlooped.Extensions.AI.Grok;
using Devlooped.Extensions.AI.OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;

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

        return new OpenAIChatClient(options.ApiKey, options.ModelId, options);
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

        return new AzureOpenAIChatClient(options.Endpoint, new AzureKeyCredential(options.ApiKey), options.ModelId, options);
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

        return new Azure.AI.Inference.ChatCompletionsClient(options.Endpoint, new AzureKeyCredential(options.ApiKey), options)
            .AsIChatClient(options.ModelId);
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

        return new GrokClient(options.ApiKey, section.Get<GrokClientOptions>() ?? new())
            .AsIChatClient(options.ModelId);
    }

    internal sealed class GrokProviderOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
    }
}
