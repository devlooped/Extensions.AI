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
sealed class OpenAIClientProvider : IClientProvider
{
    static readonly IClientFactory factory = new OpenAIClientFactory();

    public string ProviderName => "openai";

    public Uri? BaseUri => new("https://api.openai.com/");

    public string? HostSuffix => null;

    public IClientFactory GetFactory() => factory;

    class OpenAIClientFactory : IClientFactory
    {
        public IChatClient CreateChatClient(IConfigurationSection section)
        {
            var options = section.Get<OpenAIProviderOptions>() ?? new();
            Throw.IfNullOrEmpty(options.ApiKey, $"{section.Path}:apikey");
            Throw.IfNullOrEmpty(options.ModelId, $"{section.Path}:modelid");

            return new ProviderOptionsChatClient<OpenAIClientOptions>(
                new OpenAIClient(new ApiKeyCredential(options.ApiKey), options).GetChatClient(options.ModelId).AsIChatClient(),
                options);
        }

        public ISpeechToTextClient CreateSpeechToTextClient(IConfigurationSection section)
        {
            var options = section.Get<OpenAIProviderOptions>() ?? new();
            Throw.IfNullOrEmpty(options.ApiKey, $"{section.Path}:apikey");
            Throw.IfNullOrEmpty(options.ModelId, $"{section.Path}:modelid");

            return new ProviderOptionsSpeechToTextClient<OpenAIClientOptions>(
                new OpenAIClient(new ApiKeyCredential(options.ApiKey), options).GetAudioClient(options.ModelId).AsISpeechToTextClient(),
                options);
        }

        public ITextToSpeechClient CreateTextToSpeechClient(IConfigurationSection section)
        {
            var options = section.Get<OpenAIProviderOptions>() ?? new();
            Throw.IfNullOrEmpty(options.ApiKey, $"{section.Path}:apikey");
            Throw.IfNullOrEmpty(options.ModelId, $"{section.Path}:modelid");

            return new ProviderOptionsTextToSpeechClient<OpenAIClientOptions>(
                new OpenAIClient(new ApiKeyCredential(options.ApiKey), options).GetAudioClient(options.ModelId).AsITextToSpeechClient(),
                options);
        }
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
sealed class AzureOpenAIClientProvider : IClientProvider
{
    static readonly IClientFactory factory = new AzureOpenAIClientFactory();

    public string ProviderName => "azure.openai";

    public Uri? BaseUri => null;

    public string? HostSuffix => ".openai.azure.com";

    public IClientFactory GetFactory() => factory;

    class AzureOpenAIClientFactory : IClientFactory
    {
        public IChatClient CreateChatClient(IConfigurationSection section)
        {
            var options = section.Get<AzureOpenAIProviderOptions>() ?? new();
            Throw.IfNullOrEmpty(options.ApiKey, $"{section.Path}:apikey");
            Throw.IfNullOrEmpty(options.ModelId, $"{section.Path}:modelid");
            Throw.IfNull(options.Endpoint, $"{section.Path}:endpoint");

            return new ProviderOptionsChatClient<AzureOpenAIClientOptions>(
                new AzureOpenAIChatClient(options.Endpoint, new AzureKeyCredential(options.ApiKey), options.ModelId, options),
                options);
        }

        public ISpeechToTextClient CreateSpeechToTextClient(IConfigurationSection section)
        {
            var options = section.Get<AzureOpenAIProviderOptions>() ?? new();
            Throw.IfNullOrEmpty(options.ApiKey, $"{section.Path}:apikey");
            Throw.IfNullOrEmpty(options.ModelId, $"{section.Path}:modelid");
            Throw.IfNull(options.Endpoint, $"{section.Path}:endpoint");

            return new ProviderOptionsSpeechToTextClient<AzureOpenAIClientOptions>(
                new AzureOpenAIClient(options.Endpoint, new AzureKeyCredential(options.ApiKey), options)
                    .GetAudioClient(options.ModelId)
                    .AsISpeechToTextClient(),
                options);
        }

        public ITextToSpeechClient CreateTextToSpeechClient(IConfigurationSection section)
        {
            var options = section.Get<AzureOpenAIProviderOptions>() ?? new();
            Throw.IfNullOrEmpty(options.ApiKey, $"{section.Path}:apikey");
            Throw.IfNullOrEmpty(options.ModelId, $"{section.Path}:modelid");
            Throw.IfNull(options.Endpoint, $"{section.Path}:endpoint");

            return new ProviderOptionsTextToSpeechClient<AzureOpenAIClientOptions>(
                new AzureOpenAIClient(options.Endpoint, new AzureKeyCredential(options.ApiKey), options)
                    .GetAudioClient(options.ModelId)
                    .AsITextToSpeechClient(),
                options);
        }
    }

    internal sealed class AzureOpenAIProviderOptions : AzureOpenAIClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
        public Uri? Endpoint { get; set; }
    }
}

/// <summary>
/// Provides <see cref="IChatClient"/> instances for the Azure AI Inference API.
/// </summary>
sealed class AzureAIInferenceClientProvider : IClientProvider
{
    const string providerName = "azure.inference";
    static readonly IClientFactory factory = new AzureAIInferenceClientFactory();

    public string ProviderName => providerName;

    public Uri? BaseUri => new("https://ai.azure.com/");

    public string? HostSuffix => null;

    public IClientFactory GetFactory() => factory;

    class AzureAIInferenceClientFactory : IClientFactory
    {
        public IChatClient CreateChatClient(IConfigurationSection section)
        {
            var options = section.Get<AzureInferenceProviderOptions>() ?? new();
            Throw.IfNullOrEmpty(options.ApiKey, $"{section.Path}:apikey");
            Throw.IfNullOrEmpty(options.ModelId, $"{section.Path}:modelid");
            Throw.IfNull(options.Endpoint, $"{section.Path}:endpoint");

            return new ProviderOptionsChatClient<AzureAIInferenceClientOptions>(
                new ChatCompletionsClient(options.Endpoint, new AzureKeyCredential(options.ApiKey), options)
                    .AsIChatClient(options.ModelId),
                options);
        }

        public ISpeechToTextClient CreateSpeechToTextClient(IConfigurationSection section)
            => throw ClientProviderCapabilities.Unsupported(providerName, nameof(ISpeechToTextClient));

        public ITextToSpeechClient CreateTextToSpeechClient(IConfigurationSection section)
            => throw ClientProviderCapabilities.Unsupported(providerName, nameof(ITextToSpeechClient));
    }

    internal sealed class AzureInferenceProviderOptions : AzureAIInferenceClientOptions
    {
        public string? ApiKey { get; set; }
        public string? ModelId { get; set; }
        public Uri? Endpoint { get; set; }
    }
}

/// <summary>
/// Provides <see cref="IChatClient"/> instances for the Grok (xAI) API.
/// </summary>
sealed class GrokClientProvider : IClientProvider
{
    const string providerName = "xai";
    static readonly IClientFactory factory = new GrokClientFactory();

    public string ProviderName => providerName;

    public Uri? BaseUri => new("https://api.x.ai/");

    public string? HostSuffix => null;

    public IClientFactory GetFactory() => factory;

    class GrokClientFactory : IClientFactory
    {
        public IChatClient CreateChatClient(IConfigurationSection section)
        {
            var options = section.Get<GrokProviderOptions>() ?? new();
            Throw.IfNullOrEmpty(options.ApiKey, $"{section.Path}:apikey");
            Throw.IfNullOrEmpty(options.ModelId, $"{section.Path}:modelid");

            return new ProviderOptionsChatClient<GrokClientOptions>(
                new GrokClient(options.ApiKey, options).AsIChatClient(options.ModelId),
                options);
        }

        public ISpeechToTextClient CreateSpeechToTextClient(IConfigurationSection section)
        {
            var options = section.Get<GrokProviderOptions>() ?? new();
            Throw.IfNullOrEmpty(options.ApiKey, $"{section.Path}:apikey");
            Throw.IfNullOrEmpty(options.ModelId, $"{section.Path}:modelid");

            return new ProviderOptionsSpeechToTextClient<GrokClientOptions>(
                new GrokClient(options.ApiKey, options).AsISpeechToTextClient(),
                options);
        }

        public ITextToSpeechClient CreateTextToSpeechClient(IConfigurationSection section)
        {
            var options = section.Get<GrokProviderOptions>() ?? new();
            Throw.IfNullOrEmpty(options.ApiKey, $"{section.Path}:apikey");
            Throw.IfNullOrEmpty(options.ModelId, $"{section.Path}:modelid");

            return new ProviderOptionsTextToSpeechClient<GrokClientOptions>(
                new GrokClient(options.ApiKey, options).AsITextToSpeechClient(),
                options);
        }
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
        => ProviderOptions.GetService(serviceType, serviceKey, options, InnerClient.GetService);
}

sealed class ProviderOptionsSpeechToTextClient<TOptions>(ISpeechToTextClient inner, TOptions options) : DelegatingSpeechToTextClient(inner)
    where TOptions : notnull
{
    public override object? GetService(Type serviceType, object? serviceKey = null)
        => ProviderOptions.GetService(serviceType, serviceKey, options, InnerClient.GetService);
}

sealed class ProviderOptionsTextToSpeechClient<TOptions>(ITextToSpeechClient inner, TOptions options) : DelegatingTextToSpeechClient(inner)
    where TOptions : notnull
{
    public override object? GetService(Type serviceType, object? serviceKey = null)
        => ProviderOptions.GetService(serviceType, serviceKey, options, InnerClient.GetService);
}

static class ProviderOptions
{
    public static object? GetService<TOptions>(Type serviceType, object? serviceKey, TOptions options, Func<Type, object?, object?> next)
        where TOptions : notnull
        => IsOptionsRequest<TOptions>(serviceType, serviceKey) ? options : next(serviceType, serviceKey);

    static bool IsOptionsRequest<TOptions>(Type serviceType, object? serviceKey)
        => serviceType == typeof(object) ?
           serviceKey is string key && string.Equals(key, "options", StringComparison.OrdinalIgnoreCase) :
           typeof(TOptions).IsAssignableFrom(serviceType);
}

static class ClientProviderCapabilities
{
    public static NotSupportedException Unsupported(string providerName, string clientType)
        => new($"{clientType} is not supported by the '{providerName}' provider. Supported capabilities: {nameof(IChatClient)}.");
}
