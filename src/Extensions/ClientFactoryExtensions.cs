using System.ComponentModel;
using Devlooped.Extensions.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for registering client providers and factories.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ClientFactoryExtensions
{
    /// <summary>Adds the default <see cref="IClientFactory"/> and built-in providers to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="registerDefaults">Whether to register the default built-in providers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddClientFactory(this IServiceCollection services, bool registerDefaults = true)
    {
        if (registerDefaults)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientProvider, OpenAIClientProvider>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientProvider, AzureOpenAIClientProvider>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientProvider, AzureAIInferenceClientProvider>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientProvider, GrokClientProvider>());
        }

        services.TryAddSingleton<ClientFactory>();
        services.TryAddSingleton<IClientFactory>(sp => sp.GetRequiredService<ClientFactory>());

        return services;
    }

    /// <summary>Adds the default <see cref="IClientFactory"/> and built-in providers to the host application builder.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="registerDefaults">Whether to register the default built-in providers.</param>
    /// <returns>The builder for chaining.</returns>
    public static TBuilder AddClientFactory<TBuilder>(this TBuilder builder, bool registerDefaults = true) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddClientFactory(registerDefaults);
        return builder;
    }

    /// <summary>Registers a typed <see cref="IClientProvider"/> with the service collection.</summary>
    /// <typeparam name="TProvider">The provider type to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddClientProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, IClientProvider
    {
        services.AddEnumerable(ServiceDescriptor.Singleton<IClientProvider, TProvider>());
        return services;
    }

    /// <summary>Registers a typed <see cref="IClientProvider"/> with the service collection, using a factory function.</summary>
    /// <typeparam name="TProvider">The provider type to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="implementationFactory">The factory function to create the provider.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddClientProvider<TProvider>(
        this IServiceCollection services,
        Func<IServiceProvider, TProvider> implementationFactory)
        where TProvider : class, IClientProvider
    {
        services.AddEnumerable(ServiceDescriptor.Singleton<IClientProvider>(implementationFactory));
        return services;
    }

    /// <summary>Registers an inline <see cref="IClientProvider"/> with the specified name, base URI, host suffix, and factory functions.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The unique name for the provider.</param>
    /// <param name="baseUri">The optional base URI for automatic endpoint matching.</param>
    /// <param name="hostSuffix">The optional host suffix for automatic endpoint matching (e.g., ".openai.azure.com").</param>
    /// <param name="chatFactory">The factory function to create chat clients.</param>
    /// <param name="speechToTextFactory">The optional factory function to create speech-to-text clients.</param>
    /// <param name="textToSpeechFactory">The optional factory function to create text-to-speech clients.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddClientProvider(
        this IServiceCollection services,
        string name,
        Uri? baseUri,
        string? hostSuffix,
        Func<IConfigurationSection, IChatClient> chatFactory,
        Func<IConfigurationSection, ISpeechToTextClient>? speechToTextFactory = null,
        Func<IConfigurationSection, ITextToSpeechClient>? textToSpeechFactory = null)
    {
        services.AddEnumerable(ServiceDescriptor.Singleton<IClientProvider>(
            new DelegateClientProvider(name, baseUri, hostSuffix, chatFactory, speechToTextFactory, textToSpeechFactory)));
        return services;
    }

    /// <summary>Registers an inline <see cref="IClientProvider"/> with the specified name, base URI, and factory functions.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The unique name for the provider.</param>
    /// <param name="baseUri">The optional base URI for automatic endpoint matching.</param>
    /// <param name="chatFactory">The factory function to create chat clients.</param>
    /// <param name="speechToTextFactory">The optional factory function to create speech-to-text clients.</param>
    /// <param name="textToSpeechFactory">The optional factory function to create text-to-speech clients.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddClientProvider(
        this IServiceCollection services,
        string name,
        Uri? baseUri,
        Func<IConfigurationSection, IChatClient> chatFactory,
        Func<IConfigurationSection, ISpeechToTextClient>? speechToTextFactory = null,
        Func<IConfigurationSection, ITextToSpeechClient>? textToSpeechFactory = null)
        => services.AddClientProvider(name, baseUri, null, chatFactory, speechToTextFactory, textToSpeechFactory);

    /// <summary>Registers an inline <see cref="IClientProvider"/> with the specified name and factory functions.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The unique name for the provider.</param>
    /// <param name="chatFactory">The factory function to create chat clients.</param>
    /// <param name="speechToTextFactory">The optional factory function to create speech-to-text clients.</param>
    /// <param name="textToSpeechFactory">The optional factory function to create text-to-speech clients.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddClientProvider(
        this IServiceCollection services,
        string name,
        Func<IConfigurationSection, IChatClient> chatFactory,
        Func<IConfigurationSection, ISpeechToTextClient>? speechToTextFactory = null,
        Func<IConfigurationSection, ITextToSpeechClient>? textToSpeechFactory = null)
        => services.AddClientProvider(name, null, null, chatFactory, speechToTextFactory, textToSpeechFactory);

    static void AddEnumerable(this IServiceCollection services, ServiceDescriptor descriptor)
        // Use TryAddEnumerable behavior to avoid duplicates
        => services.TryAddEnumerable(descriptor);

    /// <summary>A delegate-based <see cref="IClientProvider"/> for inline registrations.</summary>
    sealed class DelegateClientProvider(
        string name,
        Uri? baseUri,
        string? hostSuffix,
        Func<IConfigurationSection, IChatClient> chatFactory,
        Func<IConfigurationSection, ISpeechToTextClient>? speechToTextFactory,
        Func<IConfigurationSection, ITextToSpeechClient>? textToSpeechFactory) : IClientProvider
    {
        public string ProviderName => name;
        public Uri? BaseUri => baseUri;
        public string? HostSuffix => hostSuffix;
        public IClientFactory GetFactory() => new DelegateClientFactory(chatFactory, speechToTextFactory, textToSpeechFactory);
    }

    sealed class DelegateClientFactory(
        Func<IConfigurationSection, IChatClient> chatFactory,
        Func<IConfigurationSection, ISpeechToTextClient>? speechToTextFactory,
        Func<IConfigurationSection, ITextToSpeechClient>? textToSpeechFactory) : IClientFactory
    {
        public IChatClient CreateChatClient(IConfigurationSection section) => chatFactory(section);
        public ISpeechToTextClient CreateSpeechToTextClient(IConfigurationSection section)
            => speechToTextFactory?.Invoke(section) ?? throw new NotSupportedException("Speech-to-text clients are not supported by this provider.");
        public ITextToSpeechClient CreateTextToSpeechClient(IConfigurationSection section)
            => textToSpeechFactory?.Invoke(section) ?? throw new NotSupportedException("Text-to-speech clients are not supported by this provider.");
    }
}