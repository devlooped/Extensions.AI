using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;

namespace Devlooped.Extensions.AI;

/// <summary>
/// Options for rendering JSON output to the console.
/// </summary>
public class JsonConsoleOptions
{
    static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Default settings for rendering JSON output to the console, which include:
    /// * <see cref="IncludeAdditionalProperties"/>: true
    /// * <see cref="InteractiveConfirm"/>: true if console is interactive, otherwise false
    /// * <see cref="InteractiveOnly"/>: true
    /// </summary>
    public static JsonConsoleOptions Default { get; } = new JsonConsoleOptions();

    /// <summary>
    /// Border kind for the JSON output panel.
    /// </summary>
    public BoxBorder Border { get; set; } = BoxBorder.Square;

    /// <summary>
    /// Border style for the JSON output panel.
    /// </summary>
    public Style BorderStyle { get; set; } = Style.Parse("grey");

    /// <summary>
    /// Whether to include additional properties in the JSON output.
    /// </summary>
    /// <remarks>
    /// See <see cref="ChatMessage.AdditionalProperties"/> and <see cref="ChatResponse.AdditionalProperties"/>.
    /// </remarks>
    public bool IncludeAdditionalProperties { get; set; } = true;

    /// <summary>
    /// Confirm whether to render JSON output to the console, if the console is interactive. If 
    /// it is non-interactive, JSON output will be rendered conditionally based on the 
    /// <see cref="InteractiveOnly"/> setting.
    /// </summary>
    public bool InteractiveConfirm { get; set; } = ConsoleExtensions.IsConsoleInteractive;

    /// <summary>
    /// Only render JSON output if the console is interactive.
    /// </summary>
    /// <remarks>
    /// This setting defaults to <see cref="true"/> to avoid cluttering non-interactive console 
    /// outputs with JSON, while also removing the need to conditionally check for console interactivity.
    /// </remarks>
    public bool InteractiveOnly { get; set; } = true;

    /// <summary>
    /// Specifies the length at which long text will be truncated.
    /// </summary>
    /// <remarks>
    /// This setting is useful for trimming long strings in the output for cases where you're more 
    /// interested in the metadata about the request and not so much in the actual content of the messages.
    /// </remarks>
    public int? TruncateLength { get; set; }

    /// <summary>
    /// Specifies the length at which long text will be wrapped automatically.
    /// </summary>
    /// <remarks>
    /// This setting is useful for ensuring that long lines of JSON text are wrapped to fit 
    /// in a narrower width in the console for easier reading.
    /// </remarks>
    public int? WrapLength { get; set; }

    internal Panel CreatePanel(string json)
    {
        // Determine if we need to pre-process the JSON string based on the settings.
        if (TruncateLength.HasValue || !IncludeAdditionalProperties)
        {
            json = JsonNode.Parse(json)?.ToShortJsonString(TruncateLength, IncludeAdditionalProperties) ?? json;
        }

        var panel = new Panel(WrapLength.HasValue ? new WrappedJsonText(json, WrapLength.Value) : new JsonText(json))
        {
            Border = Border,
            BorderStyle = BorderStyle,
        };

        return panel;
    }

    internal Panel CreatePanel(object value)
    {
        string? json = null;

        // Determine if we need to pre-process the JSON string based on the settings.
        if (TruncateLength.HasValue || !IncludeAdditionalProperties)
        {
            json = value.ToShortJsonString(TruncateLength, IncludeAdditionalProperties);
        }
        else
        {
            // i.e. we had no pre-processing to do
            json = JsonSerializer.Serialize(value, jsonOptions);
        }

        var panel = new Panel(WrapLength.HasValue ? new WrappedJsonText(json, WrapLength.Value) : new JsonText(json))
        {
            Border = Border,
            BorderStyle = BorderStyle,
        };

        return panel;
    }

#pragma warning disable CS9113 // Parameter is unread. BOGUS
    sealed class WrappedJsonText(string json, int maxWidth) : Renderable
#pragma warning restore CS9113 // Parameter is unread. BOGUS
    {
        readonly JsonText jsonText = new(json);

        protected override Measurement Measure(RenderOptions options, int maxWidth)
        {
            // Clamp the measurement to the desired maxWidth
            return new Measurement(Math.Min(maxWidth, maxWidth), Math.Min(maxWidth, maxWidth));
        }

        protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
        {
            // Render the original JSON text
            var segments = ((IRenderable)jsonText).Render(options, maxWidth).ToList();
            var wrapped = new List<Segment>();

            foreach (var segment in segments)
            {
                string text = segment.Text;
                Style style = segment.Style ?? Style.Plain;

                // Split long lines forcibly at maxWidth
                var idx = 0;
                while (idx < text.Length)
                {
                    var len = Math.Min(maxWidth, text.Length - idx);
                    wrapped.Add(new Segment(text.Substring(idx, len), style));
                    idx += len;
                    if (idx < text.Length)
                        wrapped.Add(Segment.LineBreak);
                }
            }
            return wrapped;
        }
    }
}
