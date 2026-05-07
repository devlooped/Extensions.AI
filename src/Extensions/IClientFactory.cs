using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace Devlooped.Extensions.AI;

/// <summary>A factory for creating AI clients based on configuration.</summary>
/// <remarks>
/// The factory resolves the appropriate <see cref="IClientProvider"/> using the following logic:
/// <list type="number">
///   <item><description>If the configuration section contains a <c>provider</c> key, looks up a provider by name.</description></item>
///   <item><description>Otherwise, matches the <c>endpoint</c> URI against registered providers' base URIs or host suffix, if any.</description></item>
///   <item><description>If no match is found, throws an <see cref="InvalidOperationException"/>.</description></item>
/// </list>
/// </remarks>
public interface IClientFactory
{
    /// <summary>Creates an <see cref="IChatClient"/> using the specified configuration section.</summary>
    /// <param name="section">The configuration section containing client settings including 
    /// <c>endpoint</c>, <c>apikey</c>, <c>modelid</c>, and optionally <c>provider</c>.</param>
    /// <returns>A configured <see cref="IChatClient"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no matching provider is found for the given configuration.</exception>
    IChatClient CreateChatClient(IConfigurationSection section);

    /// <summary>Creates an <see cref="ISpeechToTextClient"/> using the specified configuration section.</summary>
    /// <param name="section">The configuration section containing client settings including 
    /// <c>endpoint</c>, <c>apikey</c>, <c>modelid</c>, and optionally <c>provider</c>.</param>
    /// <returns>A configured <see cref="ISpeechToTextClient"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no matching provider is found for the given configuration.</exception>
    /// <exception cref="NotSupportedException">Thrown when the resolved provider does not support speech-to-text clients.</exception>
    ISpeechToTextClient CreateSpeechToTextClient(IConfigurationSection section);

    /// <summary>Creates an <see cref="ITextToSpeechClient"/> using the specified configuration section.</summary>
    /// <param name="section">The configuration section containing client settings including 
    /// <c>endpoint</c>, <c>apikey</c>, <c>modelid</c>, and optionally <c>provider</c>.</param>
    /// <returns>A configured <see cref="ITextToSpeechClient"/> instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no matching provider is found for the given configuration.</exception>
    /// <exception cref="NotSupportedException">Thrown when the resolved provider does not support text-to-speech clients.</exception>
    ITextToSpeechClient CreateTextToSpeechClient(IConfigurationSection section);
}
