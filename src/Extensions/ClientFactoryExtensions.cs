using System.ComponentModel;
using Devlooped.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for registering client providers and factories.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ClientFactoryExtensions
{
    /// <summary>Adds the default <see cref="ClientFactoryResolver"/> and built-in providers to the service collection.</summary>
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

        services.TryAddSingleton<ClientFactoryResolver>();
        services.TryAddSingleton<IClientFactoryResolver>(sp => sp.GetRequiredService<ClientFactoryResolver>());

        return services;
    }

    /// <summary>Adds the default <see cref="ClientFactoryResolver"/> and built-in providers to the host application builder.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="registerDefaults">Whether to register the default built-in providers.</param>
    /// <returns>The builder for chaining.</returns>
    public static TBuilder AddClientFactory<TBuilder>(this TBuilder builder, bool registerDefaults = true) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddClientFactory(registerDefaults);
        return builder;
    }

    /// <summary>Adds keyed <see cref="IClientFactory"/> registrations for configuration sections with direct API keys.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="prefix">The configuration prefix for clients. Defaults to <c>ai:clients</c>.</param>
    /// <param name="useDefaultProviders">Whether to register the default built-in providers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddClients(this IServiceCollection services, IConfiguration configuration, string prefix = "ai:clients", bool useDefaultProviders = true)
    {
        services.AddClientFactory(useDefaultProviders);

        foreach (var section in EnumerateFactorySections(configuration, prefix))
        {
            services.TryAdd(new ServiceDescriptor(typeof(IClientFactory), section.Path,
                factory: (sp, _) => sp.GetRequiredService<IClientFactoryResolver>().Resolve(section),
                ServiceLifetime.Singleton));

            services.TryAdd(new ServiceDescriptor(typeof(IClientFactory), new ServiceKey(section.Path),
                factory: (sp, _) => sp.GetRequiredKeyedService<IClientFactory>(section.Path),
                ServiceLifetime.Singleton));

            var dottedKey = section.Path.Replace(':', '.');

            services.TryAdd(new ServiceDescriptor(typeof(IClientFactory), dottedKey,
                factory: (sp, _) => sp.GetRequiredService<IClientFactoryResolver>().Resolve(section),
                ServiceLifetime.Singleton));

            services.TryAdd(new ServiceDescriptor(typeof(IClientFactory), new ServiceKey(dottedKey),
                factory: (sp, _) => sp.GetRequiredKeyedService<IClientFactory>(section.Path),
                ServiceLifetime.Singleton));
        }

        return services;
    }

    /// <summary>Adds keyed <see cref="IClientFactory"/> registrations for configuration sections with direct API keys.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="prefix">The configuration prefix for clients. Defaults to <c>ai:clients</c>.</param>
    /// <param name="useDefaultProviders">Whether to register the default built-in providers.</param>
    /// <returns>The builder for chaining.</returns>
    public static TBuilder AddClients<TBuilder>(this TBuilder builder, string prefix = "ai:clients", bool useDefaultProviders = true) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddClients(builder.Configuration, prefix, useDefaultProviders);
        return builder;
    }

    /// <summary>Registers a typed <see cref="IClientProvider"/> with the service collection.</summary>
    /// <typeparam name="TProvider">The provider type to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddClientProvider<TProvider>(this IServiceCollection services)
        where TProvider : class, IClientProvider
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientProvider, TProvider>());
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
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientProvider>(implementationFactory));
        return services;
    }

    static IEnumerable<IConfigurationSection> EnumerateFactorySections(IConfiguration configuration, string prefix)
    {
        var normalizedPrefix = prefix.TrimEnd(':') + ":";
        HashSet<string> sections = new(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in configuration.AsEnumerable().Where(x =>
            x.Value is not null &&
            x.Key.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase) &&
            x.Key.EndsWith(":apikey", StringComparison.OrdinalIgnoreCase)))
        {
            var sectionPath = string.Join(':', entry.Key.Split(':')[..^1]);
            if (sections.Add(sectionPath))
                yield return configuration.GetRequiredSection(sectionPath);
        }
    }
}
