using System.ComponentModel;
using Devlooped.Extensions.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering chat client providers and factories.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ChatClientFactoryExtensions
{
    /// <summary>
    /// Adds the default <see cref="IChatClientFactory"/> and built-in providers to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="registerDefaults">Whether to register the default built-in providers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChatClientFactory(this IServiceCollection services, bool registerDefaults = true)
    {
        if (registerDefaults)
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IChatClientProvider, OpenAIChatClientProvider>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IChatClientProvider, AzureOpenAIChatClientProvider>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IChatClientProvider, AzureAIInferenceChatClientProvider>());
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IChatClientProvider, GrokChatClientProvider>());
        }

        // Register the factory
        services.TryAddSingleton<IChatClientFactory, ChatClientFactory>();

        return services;
    }

    /// <summary>
    /// Adds the default <see cref="IChatClientFactory"/> and built-in providers to the host application builder.
    /// </summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="registerDefaults">Whether to register the default built-in providers.</param>
    /// <returns>The builder for chaining.</returns>
    public static TBuilder AddChatClientFactory<TBuilder>(this TBuilder builder, bool registerDefaults = true) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddChatClientFactory(registerDefaults);
        return builder;
    }

    /// <summary>
    /// Registers a typed <see cref="IChatClientProvider"/> with the service collection.
    /// </summary>
    /// <typeparam name="TProvider">The provider type to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChatClientProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, IChatClientProvider
    {
        services.AddEnumerable(ServiceDescriptor.Singleton<IChatClientProvider, TProvider>());
        return services;
    }

    /// <summary>
    /// Registers a typed <see cref="IChatClientProvider"/> with the service collection, 
    /// using a factory function.
    /// </summary>
    /// <typeparam name="TProvider">The provider type to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="implementationFactory">The factory function to create the provider.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChatClientProvider<TProvider>(
        this IServiceCollection services,
        Func<IServiceProvider, TProvider> implementationFactory)
        where TProvider : class, IChatClientProvider
    {
        services.AddEnumerable(ServiceDescriptor.Singleton<IChatClientProvider>(implementationFactory));
        return services;
    }

    /// <summary>
    /// Registers an inline <see cref="IChatClientProvider"/> with the specified name, 
    /// base URI, host suffix, and factory function.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The unique name for the provider.</param>
    /// <param name="baseUri">The optional base URI for automatic endpoint matching.</param>
    /// <param name="hostSuffix">The optional host suffix for automatic endpoint matching (e.g., ".openai.azure.com").</param>
    /// <param name="factory">The factory function to create chat clients.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChatClientProvider(
        this IServiceCollection services,
        string name,
        Uri? baseUri,
        string? hostSuffix,
        Func<IConfigurationSection, IChatClient> factory)
    {
        services.AddEnumerable(ServiceDescriptor.Singleton<IChatClientProvider>(
            new DelegateChatClientProvider(name, baseUri, hostSuffix, factory)));
        return services;
    }

    /// <summary>
    /// Registers an inline <see cref="IChatClientProvider"/> with the specified name, 
    /// base URI, and factory function.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The unique name for the provider.</param>
    /// <param name="baseUri">The optional base URI for automatic endpoint matching.</param>
    /// <param name="factory">The factory function to create chat clients.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChatClientProvider(
        this IServiceCollection services,
        string name,
        Uri? baseUri,
        Func<IConfigurationSection, IChatClient> factory)
        => services.AddChatClientProvider(name, baseUri, null, factory);

    /// <summary>
    /// Registers an inline <see cref="IChatClientProvider"/> with the specified name and factory function.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The unique name for the provider.</param>
    /// <param name="factory">The factory function to create chat clients.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddChatClientProvider(
        this IServiceCollection services,
        string name,
        Func<IConfigurationSection, IChatClient> factory)
        => services.AddChatClientProvider(name, null, null, factory);

    static void AddEnumerable(this IServiceCollection services, ServiceDescriptor descriptor)
    {
        // Use TryAddEnumerable behavior to avoid duplicates
        services.TryAddEnumerable(descriptor);
    }

    /// <summary>
    /// A delegate-based <see cref="IChatClientProvider"/> for inline registrations.
    /// </summary>
    sealed class DelegateChatClientProvider(string name, Uri? baseUri, string? hostSuffix, Func<IConfigurationSection, IChatClient> factory) : IChatClientProvider
    {
        public string ProviderName => name;
        public Uri? BaseUri => baseUri;
        public string? HostSuffix => hostSuffix;
        public IChatClient Create(IConfigurationSection section) => factory(section);
    }
}
