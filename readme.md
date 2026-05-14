# Devlooped AI Extensions

![Icon](assets/img/icon-32.png) Devlooped AI Extensions
============

[![Version](https://img.shields.io/nuget/vpre/Devlooped.Extensions.AI.svg?color=royalblue)](https://www.nuget.org/packages/Devlooped.Extensions.AI)
[![Downloads](https://img.shields.io/nuget/dt/Devlooped.Extensions.AI.svg?color=darkmagenta)](https://www.nuget.org/packages/Devlooped.Extensions.AI)
[![EULA](https://img.shields.io/badge/EULA-OSMF-blue?labelColor=black&color=C9FF30)](osmfeula.txt)
[![OSS](https://img.shields.io/github/license/devlooped/oss.svg?color=blue)](license.txt) 

<!-- #extensions-title -->
Extensions for Microsoft.Extensions.AI
<!-- #extensions-title -->

<!-- #extensions -->

## Overview

This package adds configuration-driven client registration, provider resolution, tool helpers, OpenAI-specific extensions, and observability helpers for `Microsoft.Extensions.AI`.

It is split into two packages:

| Package | Purpose |
| --- | --- |
| `Devlooped.Extensions.AI` | Configuration, providers, chat helpers, tools, OpenAI extras, and pipeline observability |
| `Devlooped.Extensions.AI.Console` | Rich JSON console logging for chat and HTTP pipeline messages |

## Configuration-driven clients

Register clients from configuration with `AddAIClients`:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.AddAIClients();

var app = builder.Build();
var chat = app.Services.GetChatClient("grok");
```

The default configuration prefix is `ai:clients`. A minimal configuration section looks like this:

```json
{
  "AI": {
    "Clients": {
      "Grok": {
        "provider": "xai",
        "apikey": "xai-...",
        "modelid": "grok-4-fast"
      }
    }
  }
}
```

Useful rules:

| Config shape | Registration |
| --- | --- |
| section has `apikey` | keyed `IClientFactory` |
| section has `modelid` | keyed `IChatClient` |
| section has `id` | overrides the service key |
| section has `lifetime` | controls the chat client lifetime |

`IChatClient` registrations are reloadable. `IClientFactory` registrations stay valid across configuration changes and create fresh clients each time you call `CreateChatClient`, `CreateSpeechToTextClient`, or `CreateTextToSpeechClient`.

Built-in provider support:

| Provider | Name | Chat | Speech-to-text | Text-to-speech | Match |
| --- | --- | --- | --- | --- | --- |
| OpenAI | `openai` | yes | yes | yes | explicit `provider`, or `https://api.openai.com/` |
| Azure OpenAI | `azure.openai` | yes | yes | yes | explicit `provider`, or `*.openai.azure.com` |
| Azure AI Inference | `azure.inference` | yes | no | no | explicit `provider`, or `https://ai.azure.com/` |
| xAI / Grok | `xai` | yes | yes | yes | explicit `provider`, or `https://api.x.ai/` |

When `provider` is omitted, endpoint-based matching is used. If no `endpoint` is provided at all, OpenAI is the default provider.

You can also register your own provider:

```csharp
builder.Services.AddAIClientProvider<MyClientProvider>();
// or
builder.Services.AddAIClientProvider(sp => new MyClientProvider(sp));
```

Use `useDefaultProviders: false` if you want only your own providers:

```csharp
builder.AddAIClients(useDefaultProviders: false);
```

Section-bound clients expose the provider options they were created with. Most callers can request the provider options type directly, for example:

```csharp
var options = chat.GetService<OpenAIClientOptions>();
```

For keyed lookup, use `GetChatClient`, `GetSpeechToTextClient`, and `GetTextToSpeechClient`.

## Client defaults

Use the `Configure*ClientDefaults` methods to apply shared pipelines without touching each registration site.

```csharp
builder
    .ConfigureChatClientDefaults(b => b.UseLogging())
    .ConfigureChatClientDefaults("AI:Clients:Grok", b => b.UseLogging())
    .ConfigureSpeechToTextClientDefaults(b => b.UseLogging())
    .ConfigureTextToSpeechClientDefaults(b => b.UseLogging())
    .AddAIClients();
```

Behavior:

| Rule | Meaning |
| --- | --- |
| Global defaults | apply to every client of that modality |
| Section-specific defaults | match the exact configuration section path, case-insensitively |
| Section paths | use `:` separators, not `.` |
| Order | registrations run in the order they were added |

Chat defaults survive reloads because they are applied outside the reloadable chat wrapper. Factory-created speech/chat clients get defaults applied on each `Create*` call.

## Chat helpers

`Chat` is a convenient `IList<ChatMessage>` implementation with factory helpers:

```csharp
var messages = new Chat
{
    Chat.System("You are a helpful assistant."),
    Chat.User("What is 101 * 3?")
};

var options = new ChatOptions
{
    EndUserId = "user-123"
};
```

`Chat` also supports collection initializer syntax with string roles:

```csharp
var chat = new Chat
{
    { "system", "You are concise." },
    { "user", "Say hello." }
};
```

`Chat.Developer(...)` is also available for developer-role messages.

For source-generated serialization, use `ChatJsonContext.DefaultOptions`.

## Tool calling helpers

`ToolFactory.Create` turns a delegate into an `AIFunction` with safe, snake_case tool names:

```csharp
static MyResult RunTool(string name, string description, string content) => new(name, description, content);

AIFunction tool = ToolFactory.Create(RunTool);
```

`ToolExtensions.FindCalls` locates tool invocations and their results in `ChatResponse` or message histories:

```csharp
var response = await client.GetResponseAsync(messages, options);
var call = response.FindCalls<MyResult>(tool).FirstOrDefault();

if (call is not null)
{
    Console.WriteLine(call.Result);
}
```

If you only need the raw call/result pair, use the untyped `FindCalls` overload and inspect `Outcome.Exception`.

`ToolJsonOptions.Default` provides the serializer settings used by the tool helpers.

## OpenAI extras

The `Devlooped.Extensions.AI.OpenAI` namespace adds OpenAI-specific helpers on top of `ChatOptions`.

### Verbosity

`Verbosity` is available as an extension property on `ChatOptions`:

```csharp
using Devlooped.Extensions.AI.OpenAI;

var options = new ChatOptions
{
    Verbosity = Verbosity.Low
};
```

`Verbosity` is supported by GPT-5+ models. Setting it automatically configures the raw response factory, so do not set a custom `RawRepresentationFactory` yourself when using it.

If you want a bindable options type, use `OpenAIChatOptions`.

### Web search

`WebSearchTool` wraps the OpenAI Responses API web search tool with typed location and domain controls:

```csharp
var options = new ChatOptions
{
    Tools =
    [
        new WebSearchTool("AR")
        {
            Region = "Bariloche",
            TimeZone = "America/Argentina/Buenos_Aires",
            AllowedDomains = ["catedralaltapatagonia.com"]
        }
    ]
};
```

Supported properties:

| Property | Meaning |
| --- | --- |
| `Country` | ISO alpha-2 country code |
| `Region` | Free-text region |
| `City` | Free-text city |
| `TimeZone` | IANA time zone |
| `AllowedDomains` | Domain allow-list for search results |

## Observability

`ClientPipelineExtensions` adds low-level request/response observation for any `ClientPipelineOptions`-derived type:

```csharp
var requests = new List<JsonNode>();
var responses = new List<JsonNode>();

var options = OpenAIClientOptions.Observable(requests.Add, responses.Add);
```

`Observe` adds the pipeline policy to an existing options instance; `Observable` creates a configured instance in one call. Non-JSON payloads are ignored.

<!-- #console -->
## Console logging

Install `Devlooped.Extensions.AI.Console` to get rich JSON console logging.

### Chat pipeline logging

```csharp
using Devlooped.Extensions.AI;
using Microsoft.Extensions.AI;

var chat = someChatClient
    .AsBuilder()
    .UseJsonConsoleLogging(new JsonConsoleOptions
    {
        InteractiveOnly = false,
        TruncateLength = 200
    })
    .Build();
```

### HTTP pipeline logging

```csharp
var client = new OpenAIClient(
    apiKey,
    new OpenAIClientOptions().UseJsonConsoleLogging());
```

`JsonConsoleOptions` lets you control:

| Option | Meaning |
| --- | --- |
| `Border` / `BorderStyle` | panel appearance |
| `IncludeAdditionalProperties` | include extra message/response data |
| `InteractiveConfirm` | ask before enabling logging in interactive consoles |
| `InteractiveOnly` | suppress output when the console is not interactive |
| `TruncateLength` | trim long text |
| `WrapLength` | wrap long text |

The default settings favor interactive development sessions and keep non-interactive output quiet.

<!-- #console -->

<!-- #extensions -->

<!-- include https://github.com/devlooped/.github/raw/main/osmf.md -->
## Open Source Maintenance Fee

To ensure the long-term sustainability of this project, users of this package who generate 
revenue must pay an [Open Source Maintenance Fee](https://opensourcemaintenancefee.org). 
While the source code is freely available under the terms of the [License](license.txt), 
this package and other aspects of the project require [adherence to the Maintenance Fee](osmfeula.txt).

To pay the Maintenance Fee, [become a Sponsor](https://github.com/sponsors/devlooped) at the proper 
OSMF tier. A single fee covers all of [Devlooped packages](https://www.nuget.org/profiles/Devlooped).

<!-- https://github.com/devlooped/.github/raw/main/osmf.md -->

<!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->
# Sponsors 

<!-- sponsors.md -->
[![Clarius Org](https://avatars.githubusercontent.com/u/71888636?v=4&s=39 "Clarius Org")](https://github.com/clarius)
[![MFB Technologies, Inc.](https://avatars.githubusercontent.com/u/87181630?v=4&s=39 "MFB Technologies, Inc.")](https://github.com/MFB-Technologies-Inc)
[![SandRock](https://avatars.githubusercontent.com/u/321868?u=99e50a714276c43ae820632f1da88cb71632ec97&v=4&s=39 "SandRock")](https://github.com/sandrock)
[![DRIVE.NET, Inc.](https://avatars.githubusercontent.com/u/15047123?v=4&s=39 "DRIVE.NET, Inc.")](https://github.com/drivenet)
[![Keith Pickford](https://avatars.githubusercontent.com/u/16598898?u=64416b80caf7092a885f60bb31612270bffc9598&v=4&s=39 "Keith Pickford")](https://github.com/Keflon)
[![Thomas Bolon](https://avatars.githubusercontent.com/u/127185?u=7f50babfc888675e37feb80851a4e9708f573386&v=4&s=39 "Thomas Bolon")](https://github.com/tbolon)
[![Kori Francis](https://avatars.githubusercontent.com/u/67574?u=3991fb983e1c399edf39aebc00a9f9cd425703bd&v=4&s=39 "Kori Francis")](https://github.com/kfrancis)
[![Reuben Swartz](https://avatars.githubusercontent.com/u/724704?u=2076fe336f9f6ad678009f1595cbea434b0c5a41&v=4&s=39 "Reuben Swartz")](https://github.com/rbnswartz)
[![Jacob Foshee](https://avatars.githubusercontent.com/u/480334?v=4&s=39 "Jacob Foshee")](https://github.com/jfoshee)
[![](https://avatars.githubusercontent.com/u/33566379?u=bf62e2b46435a267fa246a64537870fd2449410f&v=4&s=39 "")](https://github.com/Mrxx99)
[![Eric Johnson](https://avatars.githubusercontent.com/u/26369281?u=41b560c2bc493149b32d384b960e0948c78767ab&v=4&s=39 "Eric Johnson")](https://github.com/eajhnsn1)
[![Jonathan ](https://avatars.githubusercontent.com/u/5510103?u=98dcfbef3f32de629d30f1f418a095bf09e14891&v=4&s=39 "Jonathan ")](https://github.com/Jonathan-Hickey)
[![Ken Bonny](https://avatars.githubusercontent.com/u/6417376?u=569af445b6f387917029ffb5129e9cf9f6f68421&v=4&s=39 "Ken Bonny")](https://github.com/KenBonny)
[![Simon Cropp](https://avatars.githubusercontent.com/u/122666?v=4&s=39 "Simon Cropp")](https://github.com/SimonCropp)
[![agileworks-eu](https://avatars.githubusercontent.com/u/5989304?v=4&s=39 "agileworks-eu")](https://github.com/agileworks-eu)
[![Zheyu Shen](https://avatars.githubusercontent.com/u/4067473?v=4&s=39 "Zheyu Shen")](https://github.com/arsdragonfly)
[![Vezel](https://avatars.githubusercontent.com/u/87844133?v=4&s=39 "Vezel")](https://github.com/vezel-dev)
[![ChilliCream](https://avatars.githubusercontent.com/u/16239022?v=4&s=39 "ChilliCream")](https://github.com/ChilliCream)
[![4OTC](https://avatars.githubusercontent.com/u/68428092?v=4&s=39 "4OTC")](https://github.com/4OTC)
[![domischell](https://avatars.githubusercontent.com/u/66068846?u=0a5c5e2e7d90f15ea657bc660f175605935c5bea&v=4&s=39 "domischell")](https://github.com/DominicSchell)
[![Adrian Alonso](https://avatars.githubusercontent.com/u/2027083?u=129cf516d99f5cb2fd0f4a0787a069f3446b7522&v=4&s=39 "Adrian Alonso")](https://github.com/adalon)
[![torutek](https://avatars.githubusercontent.com/u/33917059?v=4&s=39 "torutek")](https://github.com/torutek)
[![Ryan McCaffery](https://avatars.githubusercontent.com/u/16667079?u=c0daa64bb5c1b572130e05ae2b6f609ecc912d4d&v=4&s=39 "Ryan McCaffery")](https://github.com/mccaffers)
[![Seika Logiciel](https://avatars.githubusercontent.com/u/2564602?v=4&s=39 "Seika Logiciel")](https://github.com/SeikaLogiciel)
[![Andrew Grant](https://avatars.githubusercontent.com/devlooped-user?s=39 "Andrew Grant")](https://github.com/wizardness)
[![eska-gmbh](https://avatars.githubusercontent.com/devlooped-team?s=39 "eska-gmbh")](https://github.com/eska-gmbh)


<!-- sponsors.md -->
[![Sponsor this project](https://avatars.githubusercontent.com/devlooped-sponsor?s=118 "Sponsor this project")](https://github.com/sponsors/devlooped)

[Learn more about GitHub Sponsors](https://github.com/sponsors)

<!-- https://github.com/devlooped/sponsors/raw/main/footer.md -->
