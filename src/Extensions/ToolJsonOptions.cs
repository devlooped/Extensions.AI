using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Provides a <see cref="JsonSerializerOptions"/> optimized for use with 
/// function calling and tools.
/// </summary>
public static class ToolJsonOptions
{
    static ToolJsonOptions() => Default.MakeReadOnly();

    /// <summary>
    /// Default <see cref="JsonSerializerOptions"/> for function calling and tools.
    /// </summary>
    public static JsonSerializerOptions Default { get; } = new(AIJsonUtilities.DefaultOptions)
    {
        Converters =
        {
            new AdditionalPropertiesDictionaryConverter(),
        },
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = Debugger.IsAttached || AIJsonUtilities.DefaultOptions.WriteIndented,
        TypeInfoResolver = new TypeInjectingResolver(AIJsonUtilities.DefaultOptions.TypeInfoResolverChain)
    };
}
