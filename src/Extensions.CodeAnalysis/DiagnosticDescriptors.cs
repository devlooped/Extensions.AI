using Microsoft.CodeAnalysis;

namespace Devlooped.Extensions.AI.CodeAnalysis;

static class DiagnosticDescriptors
{
    public static DiagnosticDescriptor AddChatClientsRemoved { get; } = new(
        DiagnosticIds.AddChatClientsRemoved,
        title: "AddChatClients was removed",
        messageFormat: "AddChatClients was removed; use AddAIClients and ConfigureChatClientDefaults instead",
        category: "Devlooped.Extensions.AI",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "AddChatClients was removed in favor of AddAIClients and ConfigureChatClientDefaults.");

    public static DiagnosticDescriptor ConfigureCallbackNotMigratable { get; } = new(
        DiagnosticIds.ConfigureCallbackNotMigratable,
        title: "Configure callback cannot be migrated automatically",
        messageFormat: "The configure callback cannot be migrated automatically; convert it manually to ConfigureChatClientDefaults call(s) before AddAIClients",
        category: "Devlooped.Extensions.AI",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The configure callback uses patterns that cannot be converted safely to ConfigureChatClientDefaults.");
}
