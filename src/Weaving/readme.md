<!-- #content -->
Run AI-powered C# files with the power of Microsoft.Extensions.AI and Devlooped.Extensions.AI

```csharp
#:package Weaving@0.*

// Sample X.AI client usage with .NET
var messages = new Chat()
{
    { "system", "You are a highly intelligent AI assistant." },
    { "user", "What is 101*3?" },
};

var grok = new GrokClient(Env.Get("XAI_API_KEY")!);

var options = new GrokChatOptions
{
    ModelId = "grok-3-mini", // or "grok-3-mini-fast"
    ReasoningEffort = ReasoningEffort.High, // or ReasoningEffort.Low
    Search = GrokSearch.Auto, // or GrokSearch.On or GrokSearch.Off
};

var response = await grok.GetResponseAsync(messages, options);

AnsiConsole.MarkupLine($":robot: {response.Text}");
```

> [!NOTE]
> The most useful namespaces and dependencies for developing Microsoft.Extensions.AI-
> powered applications are automatically referenced and imported when using this package.

<!-- #content -->
<!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->
<!-- exclude -->