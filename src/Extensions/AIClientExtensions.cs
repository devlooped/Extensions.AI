using System.ComponentModel;
using Devlooped.Extensions.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Extension methods for registering client providers and factories.</summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AIClientExtensions
{
    /// <summary>Adds keyed <see cref="IClientFactory"/> and <see cref="IChatClient"/> registrations from configuration.</summary>
    /// <remarks>
    /// Registers keyed <see cref="IClientFactory"/> entries by full section path for sections with a direct <c>apikey</c>, wrapped in a 
    /// <see cref="DefaultsApplyingClientFactory"/> that applies <see cref="ChatDefaultsEntry"/> (and speech/text-to-speech 
    /// equivalents) registered via <c>Configure*ClientDefaults</c>. Also registers keyed <see cref="IChatClient"/> entries
    /// by full section path for sections with a <c>modelid</c>, using <see cref="ConfigurableChatClient"/> for auto-reload
    /// support. The following alternate lookup keys are registered for each service (first registration wins):
    /// <list type="bullet">
    ///   <item>Configured <c>id</c> attribute from the section (highest-priority alias).</item>
    ///   <item>Section path, lowercase invariant (e.g., <c>ai:clients:grok</c> for <c>ai:clients:Grok</c>).</item>
    ///   <item>Section path with <c>:</c> replaced by <c>.</c> (e.g., <c>ai.clients.Grok</c>).</item>
    ///   <item>Section path with <c>:</c> replaced by <c>.</c>, lowercase (e.g., <c>ai.clients.grok</c>).</item>
    ///   <item>Last path segment (e.g., <c>Grok</c>).</item>
    ///   <item>Last path segment, lowercase (e.g., <c>grok</c>).</item>
    /// </list>
    /// Chat defaults flow through the shared factory so they are applied once, including on every configuration reload.
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="prefix">The configuration prefix for clients. Defaults to <c>ai:clients</c>.</param>
    /// <param name="useDefaultProviders">Whether to register the default built-in providers.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAIClients(this IServiceCollection services, IConfiguration configuration, string prefix = "ai:clients", bool useDefaultProviders = true)
    {
        services.AddClientFactoryResolver(useDefaultProviders);
        services.AddLogging();

        var factorySections = EnumerateFactorySections(configuration, prefix).ToList();
        foreach (var section in factorySections)
        {
            var path = section.Path;

            services.TryAdd(new ServiceDescriptor(typeof(IClientFactory), path,
                factory: (sp, _) =>
                {
                    var configurable = new ConfigurableClientFactory(
                        configuration, path,
                        sp.GetRequiredService<IClientFactoryResolver>());

                    return new DefaultsApplyingClientFactory(configurable, path, sp);
                },
                ServiceLifetime.Singleton));
        }

        foreach (var section in factorySections)
            TryAddKeyedAlias<IClientFactory>(services, section.Get<ClientOptions>()?.Id, section.Path, ServiceLifetime.Singleton);

        foreach (var section in factorySections)
        {
            foreach (var alias in GetGeneratedAliases(section.Path))
                TryAddKeyedAlias<IClientFactory>(services, alias, section.Path, ServiceLifetime.Singleton);
        }

        var normalizedPrefix = prefix.TrimEnd(':') + ":";
        var chatSections = configuration.AsEnumerable().Where(x =>
                x.Value is not null &&
                x.Key.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase) &&
                x.Key.EndsWith(":modelid", StringComparison.OrdinalIgnoreCase))
            .Select(entry =>
            {
                var path = string.Join(':', entry.Key.Split(':')[..^1]);
                var options = configuration.GetRequiredSection(path).Get<ClientOptions>();
                var defaultId = GetDefaultId(path);
                var id = string.IsNullOrEmpty(options?.Id) ? defaultId : options.Id;
                var lifetime = options?.Lifetime ?? ServiceLifetime.Singleton;

                return (Path: path, Id: id, DefaultId: defaultId, ConfiguredId: options?.Id, Lifetime: lifetime);
            })
            .ToList();

        foreach (var section in chatSections)
        {
            services.TryAdd(new ServiceDescriptor(typeof(IChatClient), section.Path,
                factory: (sp, _) =>
                {
                    // The keyed IClientFactory exists for sections with a direct apikey.
                    // For sub-sections that inherit their apikey from a parent, create a
                    // section-specific defaults-applying factory on the fly.
                    var sectionFactory = sp.GetKeyedService<IClientFactory>(section.Path)
                        ?? new DefaultsApplyingClientFactory(
                            new ConfigurableClientFactory(configuration, section.Path, sp.GetRequiredService<IClientFactoryResolver>()),
                            section.Path, sp);

                    return new ConfigurableChatClient(sectionFactory, configuration,
                        sp.GetRequiredService<ILogger<ConfigurableChatClient>>(),
                        section.Path, section.Id);
                },
                section.Lifetime));
        }

        foreach (var section in chatSections)
            TryAddKeyedAlias<IChatClient>(services, section.ConfiguredId, section.Path, section.Lifetime);

        foreach (var section in chatSections)
        {
            foreach (var alias in GetGeneratedAliases(section.Path))
                TryAddKeyedAlias<IChatClient>(services, alias, section.Path, section.Lifetime);
        }

        return services;
    }

    /// <summary>Adds keyed <see cref="IClientFactory"/> registrations by full section path for configuration sections with direct API keys.</summary>
    /// <param name="builder">The host application builder.</param>
    /// <param name="prefix">The configuration prefix for clients. Defaults to <c>ai:clients</c>.</param>
    /// <param name="useDefaultProviders">Whether to register the default built-in providers.</param>
    /// <returns>The builder for chaining.</returns>
    public static TBuilder AddAIClients<TBuilder>(this TBuilder builder, string prefix = "ai:clients", bool useDefaultProviders = true) where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddAIClients(builder.Configuration, prefix, useDefaultProviders);
        return builder;
    }

    /// <summary>Registers a typed <see cref="IClientProvider"/> with the service collection.</summary>
    /// <typeparam name="TProvider">The provider type to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAIClientProvider<TProvider>(this IServiceCollection services)
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
    public static IServiceCollection AddAIClientProvider<TProvider>(
        this IServiceCollection services,
        Func<IServiceProvider, TProvider> implementationFactory)
        where TProvider : class, IClientProvider
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientProvider>(implementationFactory));
        return services;
    }

    /// <summary>Adds the default <see cref="ClientFactoryResolver"/> and built-in providers to the service collection.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="registerDefaults">Whether to register the default built-in providers.</param>
    /// <returns>The service collection for chaining.</returns>
    static IServiceCollection AddClientFactoryResolver(this IServiceCollection services, bool registerDefaults = true)
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

    /// <summary>Gets a chat client by id from the service provider.</summary>
    public static IChatClient? GetChatClient(this IServiceProvider services, string id)
        => services.GetKeyedService<IChatClient>(id);

    /// <summary>Gets a text to speech client by id from the service provider.</summary>
    public static ITextToSpeechClient? GetTextToSpeechClient(this IServiceProvider services, string id)
        => services.GetKeyedService<ITextToSpeechClient>(id);

    /// <summary>Gets a speech to text client by id from the service provider.</summary>
    public static ISpeechToTextClient? GetSpeechToTextClient(this IServiceProvider services, string id)
        => services.GetKeyedService<ISpeechToTextClient>(id);

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

    static string GetDefaultId(string path)
    {
        var separator = path.LastIndexOf(':');
        return separator < 0 ? path : path[(separator + 1)..];
    }

    static IEnumerable<string> GetGeneratedAliases(string path)
    {
        yield return path.ToLowerInvariant();
        var dotted = path.Replace(':', '.');
        yield return dotted;
        yield return dotted.ToLowerInvariant();
        var lastSegment = GetDefaultId(path);
        yield return lastSegment;
        yield return lastSegment.ToLowerInvariant();
    }

    static void TryAddKeyedAlias<TService>(IServiceCollection services, string? id, string path, ServiceLifetime lifetime)
        where TService : class
    {
        if (string.IsNullOrEmpty(id) || string.Equals(id, path, StringComparison.Ordinal))
            return;

        services.TryAdd(new ServiceDescriptor(typeof(TService), id,
            factory: (sp, _) => sp.GetRequiredKeyedService<TService>(path),
            lifetime));
    }

    internal class ClientOptions
    {
        public string? ApiKey { get; set; }
        public string? Id { get; set; }
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;
    }
}
