![Icon](assets/img/icon-32.png) Devlooped AI Extensions
============

[![License](https://img.shields.io/github/license/devlooped/AI.svg?color=blue)](https://github.com//devlooped/AI/blob/main/license.txt)
[![Build](https://github.com/devlooped/AI/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/devlooped/AI/actions/workflows/build.yml)

Extensions for Microsoft.Agents.AI and Microsoft.Extensions.AI.

## Open Source Maintenance Fee

To ensure the long-term sustainability of this project, use of this project requires an 
[Open Source Maintenance Fee](https://opensourcemaintenancefee.org). While the source 
code is freely available under the terms of the [MIT License](./license.txt), all other aspects of the 
project --including opening or commenting on issues, participating in discussions and 
downloading releases-- require [adherence to the Maintenance Fee](./osmfeula.txt).

In short, if you use this project to generate revenue, the [Maintenance Fee is required](./osmfeula.txt).

To pay the Maintenance Fee, [become a Sponsor](https://github.com/sponsors/devlooped).

# Devlooped.Extensions.AI

[![Version](https://img.shields.io/nuget/vpre/Devlooped.Extensions.AI.svg?color=royalblue)](https://www.nuget.org/packages/Devlooped.Extensions.AI)
[![Downloads](https://img.shields.io/nuget/dt/Devlooped.Extensions.AI.svg?color=green)](https://www.nuget.org/packages/Devlooped.Extensions.AI)

<!-- #description -->
Extensions for Microsoft.Extensions.AI
<!-- #description -->

<!-- #content -->
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

var grok = new GrokChatClient(Environment.GetEnvironmentVariable("XAI_API_KEY")!, "grok-3-mini");

var options = new GrokChatOptions
{
    ModelId = "grok-3-mini-fast",           // 👈 can override the model on the client
    Temperature = 0.7f,
    ReasoningEffort = ReasoningEffort.High, // 👈 or Low
    Search = GrokSearch.Auto,               // 👈 or On/Off
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

var grok = new GrokChatClient(Environment.GetEnvironmentVariable("XAI_API_KEY")!, "grok-3");

var options = new ChatOptions
{
    Tools = [new HostedWebSearchTool()]     // 👈 equals setting GrokSearch.Auto
};

var response = await grok.GetResponseAsync(messages, options);
```

We also provide an OpenAI-compatible `WebSearchTool` that can be used to restrict 
the search to a specific country in a way that works with both Grok and OpenAI:

```csharp
var options = new ChatOptions
{
    Tools = [new WebSearchTool("AR")] // 👈 search in Argentina
};
```

This is equivalent to the following when used with a Grok client:
```csharp
var options = new ChatOptions
{
    //                                           👇 search in Argentina
    Tools = [new GrokSearchTool(GrokSearch.On) { Country = "AR" }] 
};
```

### Advanced Live Search

To configure advanced live search options, beyond the `On|Auto|Off` settings 
in `GrokChatOptions`, you can use the `GrokSearchTool` instead, which exposes 
the full breath of [live search options](https://docs.x.ai/docs/guides/live-search) 
available in the Grok API. 

```csharp
var options = new ChatOptions
{
    Tools = [new GrokSearchTool(GrokSearch.On)
    {
        FromDate = new DateOnly(2025, 1, 1),
        ToDate = DateOnly.FromDateTime(DateTime.Now),
        MaxSearchResults = 10,
        Sources =
        [
            new GrokWebSource
            {
                AllowedWebsites =
                [
                    "https://catedralaltapatagonia.com",
                    "https://catedralaltapatagonia.com/parte-de-nieve/",
                    "https://catedralaltapatagonia.com/tarifas/"
                ]
            },
        ]
    }]
};
```

> [!TIP]
> You can configure multiple sources including `GrokWebSource`, `GrokNewsSource`,
> `GrokRssSource` and `GrokXSource`, each containing granular options.

## OpenAI

The support for OpenAI chat clients provided in [Microsoft.Extensions.AI.OpenAI](https://www.nuget.org/packages/Microsoft.Extensions.AI.OpenAI) fall short in some scenarios:

* Specifying per-chat model identifier: the OpenAI client options only allow setting 
  a single model identifier for all requests, at the time the `OpenAIClient.GetChatClient` is 
  invoked.
* Setting reasoning effort: the Microsoft.Extensions.AI API does not expose a way to set reasoning 
  effort for reasoning-capable models, which is very useful for some models like `o4-mini`.

So solve both issues, this package provides an `OpenAIChatClient` that wraps the underlying 
`OpenAIClient` and allows setting the model identifier and reasoning effort per request, just 
like the above Grok examples showed:

```csharp
var messages = new Chat()
{
    { "system", "You are a highly intelligent AI assistant." },
    { "user", "What is 101*3?" },
};

IChatClient chat = new OpenAIChatClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!, "gpt-5");

var options = new ChatOptions
{
    ModelId = "gpt-5-mini",                 // 👈 can override the model on the client
    ReasoningEffort = ReasoningEffort.High, // 👈 or Medium/Low/Minimal, extension property
};

var response = await chat.GetResponseAsync(messages, options);
```

> [!TIP]
> We provide support for the newest `Minimal` reasoning effort in the just-released
> GPT-5 model family.

### Web Search

Similar to the Grok client, we provide the `WebSearchTool` to enable search customization 
in OpenAI too:

```csharp
var options = new ChatOptions
{
    //                          👇 search in Argentina, Bariloche region
    Tools = [new WebSearchTool("AR")
    {
        Region = "Bariloche",                        // 👈 Bariloche region
        TimeZone = "America/Argentina/Buenos_Aires", // 👈 IANA timezone
        ContextSize = WebSearchContextSize.High      // 👈 high search context size
    }]
};
```

> [!NOTE]
> This enables all features supported by the [Web search](https://platform.openai.com/docs/guides/tools-web-search) 
> feature in OpenAI.

If advanced search settings are not needed, you can use the built-in M.E.AI `HostedWebSearchTool` 
instead, which is a more generic tool and provides the basics out of the box.


## Observing Request/Response

The underlying HTTP pipeline provided by the Azure SDK allows setting up 
policies that can observe requests and responses. This is useful for 
monitoring the requests and responses sent to the AI service, regardless 
of the chat pipeline configuration used. 

This is added to the `OpenAIClientOptions` (or more properly, any 
`ClientPipelineOptions`-derived options) using the `Observe` method:

```csharp
var openai = new OpenAIClient(
    Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
    new OpenAIClientOptions().Observe(
        onRequest: request => Console.WriteLine($"Request: {request}"),
        onResponse: response => Console.WriteLine($"Response: {response}"),
    ));
```

You can for example trivially collect both requests and responses for 
payload analysis in tests as follows:

```csharp
var requests = new List<JsonNode>();
var responses = new List<JsonNode>();
var openai = new OpenAIClient(
    Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
    new OpenAIClientOptions().Observe(requests.Add, responses.Add));
```

We also provide a shorthand factory method that creates the options 
and observes is in a single call:

```csharp
var requests = new List<JsonNode>();
var responses = new List<JsonNode>();
var openai = new OpenAIClient(
    Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
    OpenAIClientOptions.Observable(requests.Add, responses.Add));
```

## Tool Results

Given the following tool:

```csharp
MyResult RunTool(string name, string description, string content) { ... }
```

You can use the `ToolFactory` and `FindCall<MyResult>` extension method to 
locate the function invocation, its outcome and the typed result for inspection:

```csharp
AIFunction tool = ToolFactory.Create(RunTool);
var options = new ChatOptions
{
    ToolMode = ChatToolMode.RequireSpecific(tool.Name), // 👈 forces the tool to be used
    Tools = [tool]
};

var response = await client.GetResponseAsync(chat, options);
// 👇 finds the expected result of the tool call
var result = response.FindCalls<MyResult>(tool).FirstOrDefault();

if (result != null)
{
    // Successful tool call
    Console.WriteLine($"Args: '{result.Call.Arguments.Count}'");
    MyResult typed = result.Result;
}
else
{
    Console.WriteLine("Tool call not found in response.");
}
```

If the typed result is not found, you can also inspect the raw outcomes by finding 
untyped calls to the tool and checking their `Outcome.Exception` property:

```csharp
var result = response.FindCalls(tool).FirstOrDefault();
if (result.Outcome.Exception is not null)
{
    Console.WriteLine($"Tool call failed: {result.Outcome.Exception.Message}");
}
else
{
    Console.WriteLine($"Tool call succeeded: {result.Outcome.Result}");
}
```

> [!IMPORTANT]
> The `ToolFactory` will also automatically sanitize the tool name 
> when using local functions to avoid invalid characters and honor 
> its original name.

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
    Environment.GetEnvironmentVariable("OPENAI_API_KEY")!,
    new OpenAIClientOptions().UseJsonConsoleLogging());
```

For a Grok client with search-enabled, a request would look like the following:

![](https://raw.githubusercontent.com/devlooped/Extensions.AI/main/assets/img/chatmessage.png)

Both alternatives receive an optional `JsonConsoleOptions` instance to configure 
the output, including truncating or wrapping long messages, setting panel style, 
and more.

The chat pipeline logging is added similar to other pipeline extensions:

```csharp
IChatClient client = new GrokChatClient(Environment.GetEnvironmentVariable("XAI_API_KEY")!, "grok-3-mini")
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
# Sponsors 

<!-- sponsors.md -->
[![Clarius Org](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/clarius.png "Clarius Org")](https://github.com/clarius)
[![MFB Technologies, Inc.](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/MFB-Technologies-Inc.png "MFB Technologies, Inc.")](https://github.com/MFB-Technologies-Inc)
[![DRIVE.NET, Inc.](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/drivenet.png "DRIVE.NET, Inc.")](https://github.com/drivenet)
[![Keith Pickford](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/Keflon.png "Keith Pickford")](https://github.com/Keflon)
[![Thomas Bolon](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/tbolon.png "Thomas Bolon")](https://github.com/tbolon)
[![Kori Francis](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/kfrancis.png "Kori Francis")](https://github.com/kfrancis)
[![Toni Wenzel](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/twenzel.png "Toni Wenzel")](https://github.com/twenzel)
[![Uno Platform](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/unoplatform.png "Uno Platform")](https://github.com/unoplatform)
[![Reuben Swartz](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/rbnswartz.png "Reuben Swartz")](https://github.com/rbnswartz)
[![Jacob Foshee](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/jfoshee.png "Jacob Foshee")](https://github.com/jfoshee)
[![](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/Mrxx99.png "")](https://github.com/Mrxx99)
[![Eric Johnson](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/eajhnsn1.png "Eric Johnson")](https://github.com/eajhnsn1)
[![David JENNI](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/davidjenni.png "David JENNI")](https://github.com/davidjenni)
[![Jonathan ](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/Jonathan-Hickey.png "Jonathan ")](https://github.com/Jonathan-Hickey)
[![Charley Wu](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/akunzai.png "Charley Wu")](https://github.com/akunzai)
[![Ken Bonny](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/KenBonny.png "Ken Bonny")](https://github.com/KenBonny)
[![Simon Cropp](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/SimonCropp.png "Simon Cropp")](https://github.com/SimonCropp)
[![agileworks-eu](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/agileworks-eu.png "agileworks-eu")](https://github.com/agileworks-eu)
[![Zheyu Shen](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/arsdragonfly.png "Zheyu Shen")](https://github.com/arsdragonfly)
[![Vezel](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/vezel-dev.png "Vezel")](https://github.com/vezel-dev)
[![ChilliCream](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/ChilliCream.png "ChilliCream")](https://github.com/ChilliCream)
[![4OTC](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/4OTC.png "4OTC")](https://github.com/4OTC)
[![Vincent Limo](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/v-limo.png "Vincent Limo")](https://github.com/v-limo)
[![Jordan S. Jones](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/jordansjones.png "Jordan S. Jones")](https://github.com/jordansjones)
[![domischell](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/DominicSchell.png "domischell")](https://github.com/DominicSchell)
[![Justin Wendlandt](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/jwendl.png "Justin Wendlandt")](https://github.com/jwendl)
[![Adrian Alonso](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/adalon.png "Adrian Alonso")](https://github.com/adalon)
[![Michael Hagedorn](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/Eule02.png "Michael Hagedorn")](https://github.com/Eule02)
[![torutek](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/torutek.png "torutek")](https://github.com/torutek)
[![Ryan McCaffery](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/mccaffers.png "Ryan McCaffery")](https://github.com/mccaffers)


<!-- sponsors.md -->

[![Sponsor this project](https://raw.githubusercontent.com/devlooped/sponsors/main/sponsor.png "Sponsor this project")](https://github.com/sponsors/devlooped)
&nbsp;

[Learn more about GitHub Sponsors](https://github.com/sponsors)

<!-- https://github.com/devlooped/sponsors/raw/main/footer.md -->
