using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Extensions for <see cref="JsonSerializerOptions"/> to enable type injection for object types.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class TypeInjectingResolverExtensions
{
    /// <summary>
    /// Creates a new <see cref="TypeInjectingResolver"/> that injects a $type property into object types.
    /// </summary>
    public static JsonSerializerOptions WithTypeInjection(this JsonSerializerOptions options)
    {
        if (options.IsReadOnly)
            options = new(options);

        options.TypeInfoResolver = new TypeInjectingResolver(
            JsonTypeInfoResolver.Combine([.. options.TypeInfoResolverChain]));

        return options;
    }
}

/// <summary>
/// A custom <see cref="IJsonTypeInfoResolver"/> that injects a $type property into object types
/// so they can be automatically distinguished during deserialization or inspection.
/// </summary>
public class TypeInjectingResolver(IJsonTypeInfoResolver inner) : IJsonTypeInfoResolver
{
    /// <inheritdoc />
    public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        var info = inner.GetTypeInfo(type, options);
        // The $type would already be present for polymorphic serialization.
        if (info?.Kind == JsonTypeInfoKind.Object && !info.Properties.Any(x => x.Name == "$type"))
        {
            var prop = info.CreateJsonPropertyInfo(typeof(string), "$type");
            prop.Get = obj => obj.GetType().FullName;
            prop.Order = -1000; // Ensure it is serialized first
            info.Properties.Add(prop);
        }
        return info;
    }
}
