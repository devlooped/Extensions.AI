using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides registration methods for default AI client configuration pipelines.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class AIClientDefaultsExtensions
{
    /// <summary>
    /// Registers a global chat client default pipeline.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The chat client builder configuration callback.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureChatClientDefaults(this IServiceCollection services, Action<ChatClientBuilder> configure)
        => ConfigureChatClientDefaultsCore(services, sectionPath: null, configure);

    /// <summary>
    /// Registers a chat client default pipeline for a specific configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="sectionPath">The exact section path to match (case-insensitive).</param>
    /// <param name="configure">The chat client builder configuration callback.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureChatClientDefaults(this IServiceCollection services, string sectionPath, Action<ChatClientBuilder> configure)
        => ConfigureChatClientDefaultsCore(services, sectionPath, configure);

    /// <summary>
    /// Registers a global chat client default pipeline.
    /// </summary>
    /// <typeparam name="TBuilder">The builder type.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">The chat client builder configuration callback.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static TBuilder ConfigureChatClientDefaults<TBuilder>(this TBuilder builder, Action<ChatClientBuilder> configure)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.ConfigureChatClientDefaults(configure);
        return builder;
    }

    /// <summary>
    /// Registers a chat client default pipeline for a specific configuration section.
    /// </summary>
    /// <typeparam name="TBuilder">The builder type.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <param name="sectionPath">The exact section path to match (case-insensitive).</param>
    /// <param name="configure">The chat client builder configuration callback.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static TBuilder ConfigureChatClientDefaults<TBuilder>(this TBuilder builder, string sectionPath, Action<ChatClientBuilder> configure)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.ConfigureChatClientDefaults(sectionPath, configure);
        return builder;
    }

    /// <summary>
    /// Registers a global text-to-speech client default pipeline.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The text-to-speech client builder configuration callback.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureTextToSpeechClientDefaults(this IServiceCollection services, Action<TextToSpeechClientBuilder> configure)
        => ConfigureTextToSpeechClientDefaultsCore(services, sectionPath: null, configure);

    /// <summary>
    /// Registers a text-to-speech client default pipeline for a specific configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="sectionPath">The exact section path to match (case-insensitive).</param>
    /// <param name="configure">The text-to-speech client builder configuration callback.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureTextToSpeechClientDefaults(this IServiceCollection services, string sectionPath, Action<TextToSpeechClientBuilder> configure)
        => ConfigureTextToSpeechClientDefaultsCore(services, sectionPath, configure);

    /// <summary>
    /// Registers a global text-to-speech client default pipeline.
    /// </summary>
    /// <typeparam name="TBuilder">The builder type.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">The text-to-speech client builder configuration callback.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static TBuilder ConfigureTextToSpeechClientDefaults<TBuilder>(this TBuilder builder, Action<TextToSpeechClientBuilder> configure)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.ConfigureTextToSpeechClientDefaults(configure);
        return builder;
    }

    /// <summary>
    /// Registers a text-to-speech client default pipeline for a specific configuration section.
    /// </summary>
    /// <typeparam name="TBuilder">The builder type.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <param name="sectionPath">The exact section path to match (case-insensitive).</param>
    /// <param name="configure">The text-to-speech client builder configuration callback.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static TBuilder ConfigureTextToSpeechClientDefaults<TBuilder>(this TBuilder builder, string sectionPath, Action<TextToSpeechClientBuilder> configure)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.ConfigureTextToSpeechClientDefaults(sectionPath, configure);
        return builder;
    }

    /// <summary>
    /// Registers a global speech-to-text client default pipeline.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The speech-to-text client builder configuration callback.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureSpeechToTextClientDefaults(this IServiceCollection services, Action<SpeechToTextClientBuilder> configure)
        => ConfigureSpeechToTextClientDefaultsCore(services, sectionPath: null, configure);

    /// <summary>
    /// Registers a speech-to-text client default pipeline for a specific configuration section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="sectionPath">The exact section path to match (case-insensitive).</param>
    /// <param name="configure">The speech-to-text client builder configuration callback.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection ConfigureSpeechToTextClientDefaults(this IServiceCollection services, string sectionPath, Action<SpeechToTextClientBuilder> configure)
        => ConfigureSpeechToTextClientDefaultsCore(services, sectionPath, configure);

    /// <summary>
    /// Registers a global speech-to-text client default pipeline.
    /// </summary>
    /// <typeparam name="TBuilder">The builder type.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <param name="configure">The speech-to-text client builder configuration callback.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static TBuilder ConfigureSpeechToTextClientDefaults<TBuilder>(this TBuilder builder, Action<SpeechToTextClientBuilder> configure)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.ConfigureSpeechToTextClientDefaults(configure);
        return builder;
    }

    /// <summary>
    /// Registers a speech-to-text client default pipeline for a specific configuration section.
    /// </summary>
    /// <typeparam name="TBuilder">The builder type.</typeparam>
    /// <param name="builder">The host application builder.</param>
    /// <param name="sectionPath">The exact section path to match (case-insensitive).</param>
    /// <param name="configure">The speech-to-text client builder configuration callback.</param>
    /// <returns>The host application builder for chaining.</returns>
    public static TBuilder ConfigureSpeechToTextClientDefaults<TBuilder>(this TBuilder builder, string sectionPath, Action<SpeechToTextClientBuilder> configure)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.ConfigureSpeechToTextClientDefaults(sectionPath, configure);
        return builder;
    }

    static IServiceCollection ConfigureChatClientDefaultsCore(IServiceCollection services, string? sectionPath, Action<ChatClientBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        ValidateSectionPath(sectionPath);
        services.AddSingleton(new ChatDefaultsEntry(sectionPath, configure));
        return services;
    }

    static IServiceCollection ConfigureTextToSpeechClientDefaultsCore(IServiceCollection services, string? sectionPath, Action<TextToSpeechClientBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        ValidateSectionPath(sectionPath);
        services.AddSingleton(new TextToSpeechDefaultsEntry(sectionPath, configure));
        return services;
    }

    static IServiceCollection ConfigureSpeechToTextClientDefaultsCore(IServiceCollection services, string? sectionPath, Action<SpeechToTextClientBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        ValidateSectionPath(sectionPath);
        services.AddSingleton(new SpeechToTextDefaultsEntry(sectionPath, configure));
        return services;
    }

    static void ValidateSectionPath(string? sectionPath)
    {
        if (sectionPath is null)
            return;

        if (sectionPath.Length == 0)
            throw new ArgumentException("sectionPath must not be empty.", nameof(sectionPath));

        if (sectionPath.Contains('.'))
            throw new ArgumentException("sectionPath must not contain '.'. Use ':' separated paths.", nameof(sectionPath));
    }
}

sealed record ChatDefaultsEntry(string? SectionPath, Action<ChatClientBuilder> Configure);
sealed record TextToSpeechDefaultsEntry(string? SectionPath, Action<TextToSpeechClientBuilder> Configure);
sealed record SpeechToTextDefaultsEntry(string? SectionPath, Action<SpeechToTextClientBuilder> Configure);
