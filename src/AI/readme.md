<!-- #content -->
Extensions for Microsoft.Extensions.AI

## Grok

Full support for Grok [Live Search](https://docs.x.ai/docs/guides/live-search) 
and [Reasoning](https://docs.x.ai/docs/guides/reasoning) model options.

```csharp
// Sample X.AI client usage with .NET
var messages = new Chat()
{
    { "system", "You are a highly intelligent AI assistant." },
    { "user", "What is 101*3?" },
};

IChatClient grok = new GrokClient(Env.Get("XAI_API_KEY")!)
    .GetChatClient("grok-3-mini")
    .AsIChatClient();

var options = new GrokChatOptions
{
    ModelId = "grok-3-mini-fast",           // can override the model on the client
    Temperature = 0.7f,
    ReasoningEffort = ReasoningEffort.High, // or ReasoningEffort.Low
    Search = GrokSearch.Auto,               // or GrokSearch.On or GrokSearch.Off
};

var response = await grok.GetResponseAsync(messages, options);
```

Search can alternatively be configured using a regular `ChatOptions` 
and adding the `HostedWebSearchTool` to the tools collection, which 
sets the live search mode to `auto` like above:

```csharp
var messages = new Chat()
{
    { "system", "You are an AI assistant that knows how to search the web." },
    { "user", "What's Tesla stock worth today? Search X and the news for latest info." },
};

var grok = new GrokClient(Env.Get("XAI_API_KEY")!)
    .GetChatClient("grok-3")
    .AsIChatClient();

var options = new ChatOptions
{
    Tools = [new HostedWebSearchTool()]
};

var response = await grok.GetResponseAsync(messages, options);
```

## Console Logging

Additional `UseJsonConsoleLogging` extension for rich JSON-formatted console logging of AI requests 
are provided at two levels: 

* Chat pipeline: similar to `UseLogging`.
* HTTP pipeline: lowest possible layer before the request is sent to the AI service, 
  can capture all requests and responses. Can also be used with other Azure SDK-based 
  clients that leverage `ClientPipelineOptions`.

> [!NOTE]
> Rich JSON formatting is provided by [Spectre.Console](https://spectreconsole.net/)

The HTTP pipeline logging can be enabled by calling `UseJsonConsoleLogging` on the
client options passed to the client constructor:

```csharp
var openai = new OpenAIClient(
    Env.Get("OPENAI_API_KEY")!,
    new OpenAIClientOptions().UseJsonConsoleLogging());
```

For a Grok client with search-enabled, a request would look like the following:

![](https://raw.githubusercontent.com/devlooped/Extensions.AI/main/assets/img/chatmessage.png)

Both alternatives receive an optional `JsonConsoleOptions` instance to configure 
the output, including truncating or wrapping long messages, setting panel style, 
and more.

The chat pipeline logging is added similar to other pipeline extensions:

```csharp
IChatClient client = new GrokClient(Env.Get("XAI_API_KEY")!)
    .GetChatClient("grok-3-mini")
    .AsIChatClient()
    .AsBuilder()
    .UseOpenTelemetry()
    // other extensions...
    .UseJsonConsoleLogging(new JsonConsoleOptions()
    {
        // Formatting options...
        Border = BoxBorder.None,
        WrapLength = 80,
    })
    .Build();
```

<!-- #content -->
<!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->
<!-- exclude -->