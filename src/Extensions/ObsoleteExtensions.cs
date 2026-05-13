using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Obsolete.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ConfigurableChatClientExtensions
{
    /// <summary>Obsolete. Use the new AddAIClients method.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use the new AddAIClients method.", true, DiagnosticId = "DEAI001")]
    public static TBuilder AddChatClients<TBuilder>(this TBuilder builder, Action<string, ChatClientBuilder>? configure = default, string prefix = "ai:clients", bool useDefaultProviders = true)
        where TBuilder : IHostApplicationBuilder
        => throw new NotSupportedException();

    /// <summary>Obsolete. Use the new AddAIClients method.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use the new AddAIClients method.", true, DiagnosticId = "DEAI001")]
    public static IServiceCollection AddChatClients(this IServiceCollection services, IConfiguration configuration, Action<string, ChatClientBuilder>? configure = default, string prefix = "ai:clients", bool useDefaultProviders = true)
        => throw new NotSupportedException();
}