using System.Text.Json;
using System.Text.Json.Nodes;

namespace Devlooped.Extensions.AI;

static class JsonExtensions
{
    static readonly JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.Web);

    /// <summary>
    /// Recursively truncates long strings in an object before serialization and optionally excludes additional properties.
    /// </summary>
    public static string ToJsonString(this object? value, int? maxStringLength = 100, bool includeAdditionalProperties = true)
    {
        if (value is null)
            return "{}";

        var node = JsonSerializer.SerializeToNode(value, value.GetType(), options);
        return FilterNode(node, maxStringLength, includeAdditionalProperties)?.ToJsonString() ?? "{}";
    }

    static JsonNode? FilterNode(JsonNode? node, int? maxStringLength = 100, bool includeAdditionalProperties = true)
    {
        if (node is JsonObject obj)
        {
            var filtered = new JsonObject();
            foreach (var prop in obj)
            {
                if (!includeAdditionalProperties && prop.Key == "AdditionalProperties")
                    continue;
                if (FilterNode(prop.Value, maxStringLength, includeAdditionalProperties) is JsonNode value)
                    filtered[prop.Key] = value.DeepClone();
            }
            return filtered;
        }
        if (node is JsonArray arr)
        {
            var filtered = new JsonArray();
            foreach (var item in arr)
            {
                if (FilterNode(item, maxStringLength, includeAdditionalProperties) is JsonNode value)
                    filtered.Add(value.DeepClone());
            }

            return filtered;
        }
        if (maxStringLength != null &&
            node is JsonValue val &&
            val.TryGetValue(out string? str) &&
            str is not null && str.Length > maxStringLength)
        {
            return str[..maxStringLength.Value] + "...";
        }
        return node;
    }
}