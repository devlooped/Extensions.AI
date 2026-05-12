using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Devlooped.Extensions.AI;

sealed class DefaultsApplyingClientFactory(IClientFactory inner, string sectionPath, IServiceProvider sp) : IClientFactory
{
    public IChatClient CreateChatClient()
    {
        var client = inner.CreateChatClient();
        var entries = sp.GetServices<ChatDefaultsEntry>()
            .Where(e => e.SectionPath == null ||
                string.Equals(e.SectionPath, sectionPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (entries.Count == 0)
            return client;

        var builder = client.AsBuilder();
        foreach (var entry in entries)
            entry.Configure(builder);

        return builder.Build(sp);
    }

    public ISpeechToTextClient CreateSpeechToTextClient()
    {
        var client = inner.CreateSpeechToTextClient();
        var entries = sp.GetServices<SpeechToTextDefaultsEntry>()
            .Where(e => e.SectionPath == null ||
                string.Equals(e.SectionPath, sectionPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (entries.Count == 0)
            return client;

        var builder = client.AsBuilder();
        foreach (var entry in entries)
            entry.Configure(builder);

        return builder.Build(sp);
    }

    public ITextToSpeechClient CreateTextToSpeechClient()
    {
        var client = inner.CreateTextToSpeechClient();
        var entries = sp.GetServices<TextToSpeechDefaultsEntry>()
            .Where(e => e.SectionPath == null ||
                string.Equals(e.SectionPath, sectionPath, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (entries.Count == 0)
            return client;

        var builder = client.AsBuilder();
        foreach (var entry in entries)
            entry.Configure(builder);

        return builder.Build(sp);
    }
}
