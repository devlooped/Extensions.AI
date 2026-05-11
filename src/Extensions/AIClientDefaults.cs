namespace Devlooped.Extensions.AI;

using Microsoft.Extensions.AI;

sealed record ChatDefaultsEntry(string? SectionPath, Action<ChatClientBuilder> Configure);
sealed record TextToSpeechDefaultsEntry(string? SectionPath, Action<TextToSpeechClientBuilder> Configure);
sealed record SpeechToTextDefaultsEntry(string? SectionPath, Action<SpeechToTextClientBuilder> Configure);
