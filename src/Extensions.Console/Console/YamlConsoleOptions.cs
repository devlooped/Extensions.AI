using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.AI;
using Spectre.Console;
using Spectre.Console.Rendering;
using YamlDotNet.Serialization;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Options for rendering YAML output to the console.
/// </summary>
public class YamlConsoleOptions
{
    static readonly JsonSerializerOptions serializationOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition =
            JsonIgnoreCondition.WhenWritingNull |
            JsonIgnoreCondition.WhenWritingDefault
    };

    static readonly ISerializer serializer = new SerializerBuilder().Build();

    /// <summary>
    /// Default settings for rendering YAML output to the console, which include:
    /// * <see cref="IncludeAdditionalProperties"/>: true
    /// * <see cref="InteractiveConfirm"/>: true if console is interactive, otherwise false
    /// * <see cref="InteractiveOnly"/>: true
    /// </summary>
    public static YamlConsoleOptions Default { get; } = new YamlConsoleOptions();

    /// <summary>
    /// Border kind for the YAML output panel.
    /// </summary>
    public BoxBorder Border { get; set; } = BoxBorder.Square;

    /// <summary>
    /// Border style for the YAML output panel.
    /// </summary>
    public Style BorderStyle { get; set; } = Style.Parse("grey");

    /// <summary>
    /// Whether to include additional properties in the YAML output.
    /// </summary>
    /// <remarks>
    /// See <see cref="ChatMessage.AdditionalProperties"/> and <see cref="ChatResponse.AdditionalProperties"/>.
    /// </remarks>
    public bool IncludeAdditionalProperties { get; set; } = true;

    /// <summary>
    /// Confirm whether to render YAML output to the console, if the console is interactive. If 
    /// it is non-interactive, YAML output will be rendered conditionally based on the 
    /// <see cref="InteractiveOnly"/> setting.
    /// </summary>
    public bool InteractiveConfirm { get; set; } = ConsoleExtensions.IsConsoleInteractive;

    /// <summary>
    /// Only render YAML output if the console is interactive.
    /// </summary>
    /// <remarks>
    /// This setting defaults to <see cref="true"/> to avoid cluttering non-interactive console 
    /// outputs with YAML, while also removing the need to conditionally check for console interactivity.
    /// </remarks>
    public bool InteractiveOnly { get; set; } = true;

    /// <summary>
    /// Specifies the length at which long text will be truncated.
    /// </summary>
    public int? TruncateLength { get; set; }

    /// <summary>
    /// Specifies the length at which long text will be wrapped automatically.
    /// </summary>
    public int? WrapLength { get; set; }

    internal Panel CreatePanel(object value)
    {
        var yaml = ToYamlString(value);

        return new Panel(WrapLength.HasValue ? new WrappedText(yaml, WrapLength.Value) : new Text(yaml, Style.Plain))
        {
            Border = Border,
            BorderStyle = BorderStyle,
        };
    }

    internal string ToYamlString(object? value)
    {
        if (value is null)
            return string.Empty;

        JsonNode? node = value switch
        {
            JsonNode existing => existing,
            _ => JsonSerializer.SerializeToNode(value, value.GetType(), serializationOptions),
        };

        if (node is null)
            return string.Empty;

        if (TruncateLength.HasValue || !IncludeAdditionalProperties)
            node = JsonNode.Parse(node.ToShortJsonString(TruncateLength, IncludeAdditionalProperties));

        var yaml = serializer.Serialize(ToPlain(node));
        return yaml.TrimEnd();
    }

    static object? ToPlain(JsonNode? node)
    {
        return node switch
        {
            null => null,
            JsonValue value => GetValue(value),
            JsonObject obj => obj.ToDictionary(kv => kv.Key, kv => ToPlain(kv.Value)),
            JsonArray arr => arr.Select(ToPlain).ToArray(),
            _ => node.ToJsonString()
        };
    }

    static object? GetValue(JsonValue value)
    {
        if (value.TryGetValue(out bool boolean))
            return boolean;
        if (value.TryGetValue(out int number))
            return number;
        if (value.TryGetValue(out long longNumber))
            return longNumber;
        if (value.TryGetValue(out double real))
            return real;
        if (value.TryGetValue(out string? str))
            return str;

        return value.GetValue<object?>();
    }

#pragma warning disable CS9113 // Parameter is unread. BOGUS
    sealed class WrappedText(string text, int maxWidth) : Renderable
#pragma warning restore CS9113 // Parameter is unread. BOGUS
    {
        readonly Text plainText = new(text, Style.Plain);

        protected override Measurement Measure(RenderOptions options, int maxWidth)
        {
            return new Measurement(Math.Min(maxWidth, this.maxWidth), Math.Min(maxWidth, this.maxWidth));
        }

        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            var segments = ((IRenderable)plainText).Render(options, maxWidth).ToList();
            var wrapped = new List<Segment>();

            foreach (var segment in segments)
            {
                if (segment.IsLineBreak)
                {
                    wrapped.Add(Segment.LineBreak);
                    continue;
                }

                var value = segment.Text;
                var style = segment.Style ?? Style.Plain;

                var idx = 0;
                while (idx < value.Length)
                {
                    var len = Math.Min(this.maxWidth, value.Length - idx);
                    wrapped.Add(new Segment(value.Substring(idx, len), style));
                    idx += len;
                    if (idx < value.Length)
                        wrapped.Add(Segment.LineBreak);
                }
            }

            return wrapped;
        }
    }
}
