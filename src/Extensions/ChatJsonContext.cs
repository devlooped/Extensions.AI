using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI;

[JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull | JsonIgnoreCondition.WhenWritingDefault,
    UseStringEnumConverter = true,
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower
#if DEBUG
    , WriteIndented = true
#endif
)]
[JsonSerializable(typeof(Chat))]
[JsonSerializable(typeof(ChatResponse))]
[JsonSerializable(typeof(ChatMessage))]
[JsonSerializable(typeof(AdditionalPropertiesDictionary))]
public partial class ChatJsonContext : JsonSerializerContext
{
    static readonly Lazy<JsonSerializerOptions> options = new(CreateDefaultOptions);

    /// <summary>
    /// Provides a pre-configured instance of <see cref="JsonSerializerOptions"/> that aligns with the context's settings.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions { get => options.Value; }

    [UnconditionalSuppressMessage("AotAnalysis", "IL3050", Justification = "DefaultJsonTypeInfoResolver is only used when reflection-based serialization is enabled")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "DefaultJsonTypeInfoResolver is only used when reflection-based serialization is enabled")]
    static JsonSerializerOptions CreateDefaultOptions()
    {
        JsonSerializerOptions options = new(Default.Options)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            WriteIndented = Debugger.IsAttached,
        };

        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
            // If reflection-based serialization is enabled by default, use it as a fallback for all other types.
            // Also turn on string-based enum serialization for all unknown enums.
            options.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver());
            options.Converters.Add(new JsonStringEnumConverter());
        }

        // Make sure we deserialize AdditionalProperties values to their primitive types
        options.Converters.Insert(0, new AdditionalPropertiesDictionaryConverter());

        options.MakeReadOnly();
        return options;
    }
}
