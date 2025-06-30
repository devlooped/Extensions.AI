using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Weaving;

/// <summary>
/// Allows retrieving configuration settings for the app environment using the 
/// default configuration system provided by <see cref="Host.CreateApplicationBuilder()"/>
/// </summary>
public static class Env
{
    static readonly IConfigurationManager configuration =
        Host.CreateApplicationBuilder(Environment.GetCommandLineArgs()).Configuration;

    /// <summary>
    /// Gets a (possibly null) configuration value for the given key.
    /// </summary>
    /// <param name="key">The key to retrieve, following .NET configuration system conventions.</param>
    public static string? Get(string key) => configuration[key];

    /// <summary>
    /// Gets a configuration value for the given key, returning a default value if the key is not found.
    /// </summary>
    /// <param name="key">The key to retrieve, following .NET configuration system conventions.</param>
    /// <param name="defaultValue">Default value to return if the key is not found.</param>
    /// <returns>Never returns null if <paramref name="defaultValue"/> is not <see langword="null"/>.</returns>
    [return: NotNullIfNotNull(nameof(defaultValue))]
    public static string? Get(string key, string defaultValue) => configuration[key] ?? defaultValue;
}
