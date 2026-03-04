using Devlooped.Extensions.AI;
using Spectre.Console;

namespace Devlooped;

public class ConsoleLoggingTests
{
    [Fact]
    public void YamlOptionsExcludeAdditionalProperties()
    {
        var options = new YamlConsoleOptions
        {
            IncludeAdditionalProperties = false,
            TruncateLength = 5
        };

        var yaml = options.ToYamlString(new
        {
            Message = "123456789",
            AdditionalProperties = new { Ignored = "value" }
        });

        Assert.DoesNotContain("AdditionalProperties", yaml);
        Assert.Contains("12345...", yaml);
        Assert.Contains("Message", yaml);
    }

    [Fact]
    public void YamlPanelRespectsCustomization()
    {
        var options = new YamlConsoleOptions
        {
            Border = BoxBorder.Ascii,
            BorderStyle = Style.Parse("red"),
            WrapLength = 10
        };

        var panel = options.CreatePanel(new { Message = "hello" });

        Assert.Equal(BoxBorder.Ascii, panel.Border);
        Assert.Equal(Style.Parse("red"), panel.BorderStyle);
    }
}
