![Icon](assets/img/icon-64.png) Devlooped.Extensions.AI
============

[![Version](https://img.shields.io/nuget/vpre/Devlooped.Extensions.AI.svg?color=royalblue)](https://www.nuget.org/packages/Devlooped.Extensions.AI)
[![Downloads](https://img.shields.io/nuget/dt/Devlooped.Extensions.AI.svg?color=green)](https://www.nuget.org/packages/Devlooped.Extensions.AI)
[![License](https://img.shields.io/github/license/devlooped/Extensions.AI.svg?color=blue)](https://github.com//devlooped/Extensions.AI/blob/main/license.txt)
[![Build](https://github.com/devlooped/Extensions.AI/actions/workflows/build.yml/badge.svg?branch=main)](https://github.com/devlooped/Extensions.AI/actions/workflows/build.yml)

# Extensions

<!-- include src/AI/readme.md#content -->
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

var grok = new GrokClient(Env.Get("XAI_API_KEY")!);

var options = new GrokChatOptions
{
    ModelId = "grok-3-mini", // or "grok-3-mini-fast"
    Temperature = 0.7f,
    ReasoningEffort = ReasoningEffort.High, // or ReasoningEffort.Low
    Search = GrokSearch.Auto, // or GrokSearch.On or GrokSearch.Off
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

var grok = new GrokClient(Env.Get("XAI_API_KEY")!);

var options = new ChatOptions
{
    ModelId = "grok-3",
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
<!-- src/AI/readme.md#content -->

# Weaving

<!-- include src/Weaving/readme.md#content -->
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

## Configuration / Environment Variables

The `Env` class provides access to the following variables/configuration automatically: 

* `.env` files: in local and parent directories
* `~/.env` file: in the user's home directory (`%userprofile%\.env` on Windows)
* All default configuration sources from [App Builder](https://learn.microsoft.com/en-us/dotnet/core/extensions/generic-host?tabs=appbuilder#host-builder-settings): 
    * Environment variables prefixed with DOTNET_.
    * Command-line arguments.
    * appsettings.json.
    * appsettings.{Environment}.json.
    * Secret Manager when the app runs in the Development environment.
    * Environment variables.
    * Command-line arguments.

<!-- #content -->
<!-- src/Weaving/readme.md#content -->

<!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->
# Sponsors 

<!-- sponsors.md -->
[![Clarius Org](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/clarius.png "Clarius Org")](https://github.com/clarius)
[![MFB Technologies, Inc.](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/MFB-Technologies-Inc.png "MFB Technologies, Inc.")](https://github.com/MFB-Technologies-Inc)
[![Torutek](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/torutek-gh.png "Torutek")](https://github.com/torutek-gh)
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
[![sorahex](https://raw.githubusercontent.com/devlooped/sponsors/main/.github/avatars/sorahex.png "sorahex")](https://github.com/sorahex)
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


<!-- sponsors.md -->

[![Sponsor this project](https://raw.githubusercontent.com/devlooped/sponsors/main/sponsor.png "Sponsor this project")](https://github.com/sponsors/devlooped)
&nbsp;

[Learn more about GitHub Sponsors](https://github.com/sponsors)

<!-- https://github.com/devlooped/sponsors/raw/main/footer.md -->
