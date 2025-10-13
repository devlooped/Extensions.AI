using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

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
    public static JsonSerializerOptions Default { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new AdditionalPropertiesDictionaryConverter(),
            new JsonStringEnumConverter(),
        },
        DefaultIgnoreCondition =
            JsonIgnoreCondition.WhenWritingDefault |
            JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = Debugger.IsAttached,
        TypeInfoResolver = new TypeInjectingResolver(new DefaultJsonTypeInfoResolver())
    };
}
