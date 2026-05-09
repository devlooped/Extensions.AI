using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;

namespace Devlooped.Extensions.AI;

/// <summary>A factory for creating AI clients based on a bound configuration section.</summary>
/// <remarks>
/// Instances are created by an <see cref="IClientProvider"/> for a specific
/// <see cref="IConfigurationSection"/>, then reused to create clients against that section.
/// </remarks>
public interface IClientFactory
{
    /// <summary>Creates an <see cref="IChatClient"/> using the bound configuration section.</summary>
    /// <returns>A configured <see cref="IChatClient"/> instance.</returns>
    IChatClient CreateChatClient();

    /// <summary>Creates an <see cref="ISpeechToTextClient"/> using the bound configuration section.</summary>
    /// <returns>A configured <see cref="ISpeechToTextClient"/> instance.</returns>
    /// <exception cref="NotSupportedException">Thrown when the resolved provider does not support speech-to-text clients.</exception>
    ISpeechToTextClient CreateSpeechToTextClient();

    /// <summary>Creates an <see cref="ITextToSpeechClient"/> using the bound configuration section.</summary>
    /// <returns>A configured <see cref="ITextToSpeechClient"/> instance.</returns>
    /// <exception cref="NotSupportedException">Thrown when the resolved provider does not support text-to-speech clients.</exception>
    ITextToSpeechClient CreateTextToSpeechClient();
}
