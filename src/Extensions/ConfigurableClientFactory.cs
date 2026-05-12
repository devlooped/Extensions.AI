using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace Devlooped.Extensions.AI;

/// <summary>
/// An <see cref="IClientFactory"/> that re-resolves the current provider and configuration section
/// on every <c>Create*Client</c> call so that a singleton factory reference remains valid across
/// configuration changes, including provider switches.
/// </summary>
sealed class ConfigurableClientFactory(IConfiguration configuration, string sectionPath, IClientFactoryResolver resolver) : IClientFactory
{
    public IChatClient CreateChatClient() => GetEffectiveFactory().CreateChatClient();

    public ISpeechToTextClient CreateSpeechToTextClient() => GetEffectiveFactory().CreateSpeechToTextClient();

    public ITextToSpeechClient CreateTextToSpeechClient() => GetEffectiveFactory().CreateTextToSpeechClient();

    IClientFactory GetEffectiveFactory() => resolver.Resolve(GetEffectiveSection());

    /// <summary>
    /// Resolves the current configuration section with API key indirection and
    /// parent-section inheritance applied.
    /// </summary>
    internal IConfigurationSection GetEffectiveSection()
    {
        var configSection = configuration.GetRequiredSection(sectionPath);

        var apikey = configSection["apikey"];
        // If the key is a path reference (contains '.' or ':'), resolve it from config
        if (apikey?.Contains('.') == true || apikey?.Contains(':') == true)
            apikey = configuration[apikey.Replace('.', ':')] ?? configuration[apikey.Replace('.', ':') + ":apikey"];

        var keysection = sectionPath;
        // Inherit apikey from parent sections when not set directly
        while (string.IsNullOrEmpty(apikey))
        {
            keysection = string.Join(':', keysection.Split(':')[..^1]);
            if (string.IsNullOrEmpty(keysection))
                break;

            apikey = configuration[$"{keysection}:apikey"];
        }

        return new ApiKeyConfigurationSection(configSection, apikey);
    }
}

/// <summary>A configuration section wrapper that overrides the apikey value with a resolved value.</summary>
sealed class ApiKeyConfigurationSection(IConfigurationSection inner, string? apiKey) : IConfigurationSection
{
    public string? this[string key]
    {
        get => string.Equals(key, "apikey", StringComparison.OrdinalIgnoreCase) && apiKey != null
            ? apiKey
            : inner[key];
        set => inner[key] = value;
    }

    public string Key => inner.Key;
    public string Path => inner.Path;
    public string? Value
    {
        get => inner.Value;
        set => inner.Value = value;
    }
    public IEnumerable<IConfigurationSection> GetChildren()
    {
        var hasApiKey = false;

        foreach (var child in inner.GetChildren())
        {
            if (string.Equals(child.Key, "apikey", StringComparison.OrdinalIgnoreCase))
            {
                hasApiKey = true;
                yield return new ApiKeyValueConfigurationSection(child, apiKey);
            }
            else
            {
                yield return child;
            }
        }

        if (!hasApiKey && apiKey is not null)
            yield return new ApiKeyValueConfigurationSection(inner.GetSection("apikey"), apiKey);
    }

    public IChangeToken GetReloadToken() => inner.GetReloadToken();
    public IConfigurationSection GetSection(string key)
        => string.Equals(key, "apikey", StringComparison.OrdinalIgnoreCase)
            ? new ApiKeyValueConfigurationSection(inner.GetSection(key), apiKey)
            : inner.GetSection(key);

    sealed class ApiKeyValueConfigurationSection(IConfigurationSection inner, string? resolvedApiKey) : IConfigurationSection
    {
        public string? this[string key]
        {
            get => inner[key];
            set => inner[key] = value;
        }

        public string Key => inner.Key;
        public string Path => inner.Path;
        public string? Value
        {
            get => resolvedApiKey ?? inner.Value;
            set => inner.Value = value;
        }

        public IEnumerable<IConfigurationSection> GetChildren() => inner.GetChildren();
        public IChangeToken GetReloadToken() => inner.GetReloadToken();
        public IConfigurationSection GetSection(string key) => inner.GetSection(key);
    }
}