![Icon](assets/img/icon-32.png) Devlooped AI Extensions
============

[![EULA](https://img.shields.io/badge/EULA-OSMF-blue?labelColor=black&color=C9FF30)](osmfeula.txt)
[![OSS](https://img.shields.io/github/license/devlooped/oss.svg?color=blue)](license.txt) 
[![Version](https://img.shields.io/nuget/vpre/Devlooped.Extensions.AI.svg?color=royalblue)](https://www.nuget.org/packages/Devlooped.Extensions.AI)
[![Downloads](https://img.shields.io/nuget/dt/Devlooped.Extensions.AI.svg?color=green)](https://www.nuget.org/packages/Devlooped.Extensions.AI)

Extensions for Microsoft.Extensions.AI.

<!-- include https://github.com/devlooped/.github/raw/main/osmf.md -->
## Open Source Maintenance Fee

To ensure the long-term sustainability of this project, users of this package who generate 
revenue must pay an [Open Source Maintenance Fee](https://opensourcemaintenancefee.org). 
While the source code is freely available under the terms of the [License](license.txt), 
this package and other aspects of the project require [adherence to the Maintenance Fee](osmfeula.txt).

To pay the Maintenance Fee, [become a Sponsor](https://github.com/sponsors/devlooped) at the proper 
OSMF tier. A single fee covers all of [Devlooped packages](https://www.nuget.org/profiles/Devlooped).

<!-- https://github.com/devlooped/.github/raw/main/osmf.md -->

<!-- #extensions-title -->
Extensions for Microsoft.Extensions.AI
<!-- #extensions-title -->

<!-- #extensions -->
## Configurable Chat Clients

Since tweaking chat options such as model identifier, reasoning effort, verbosity 
and other model settings is very common, this package provides the ability to 
drive those settings from configuration (with auto-reload support), both per-client 
as well as per-request. This makes local development and testing much easier and 
boosts the dev loop:

```json
{
  "AI": {
    "Clients": {
      "Grok": {
        "Endpoint": "https://api.grok.ai/v1",
        "ModelId": "grok-4-fast-non-reasoning",
        "ApiKey": "xai-asdf"
      }
    }
  }
}
````

```csharp
var host = new HostApplicationBuilder(args);
host.Configuration.AddJsonFile("appsettings.json, optional: false, reloadOnChange: true);
host.AddChatClients();

var app = host.Build();
var grok = app.Services.GetRequiredKeyedService<IChatClient>("Grok");
```

Changing the `appsettings.json` file will automatically update the client 
configuration without restarting the application.

## OpenAI

The support for OpenAI chat clients provided in [Microsoft.Extensions.AI.OpenAI](https://www.nuget.org/packages/Microsoft.Extensions.AI.OpenAI) fall short in some scenarios:

* Specifying per-chat model identifier: the OpenAI client options only allow setting 
  a single model identifier for all requests, at the time the `OpenAIClient.GetChatClient` is 
  invoked.
* Setting reasoning effort: the Microsoft.Extensions.AI API does not expose a way to set reasoning 
  effort for reasoning-capable models, which is very useful for some models like `gpt-5.2`.

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
    ReasoningEffort = ReasoningEffort.High, // 👈 or Medium/Low/Minimal/None, extension property
};

var response = await chat.GetResponseAsync(messages, options);
```

> [!TIP]
> We provide support for the newest `Minimal` reasoning effort in the just-released
> GPT-5 model family as well as `None` which is the new default in GPT-5.2.

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
        ContextSize = WebSearchToolContextSize.High      // 👈 high search context size
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

Both alternatives receive an optional `JsonConsoleOptions` instance to configure 
the output, including truncating or wrapping long messages, setting panel style, 
and more.

The chat pipeline logging is added similar to other pipeline extensions:

```csharp
IChatClient chat = new OpenAIChatClient(Environment.GetEnvironmentVariable("OPENAI_API_KEY")!, "gpt-5.2");
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
<!-- #extensions -->

# Devlooped.Extensions.AI.Grok

[![Version](https://img.shields.io/nuget/vpre/Devlooped.Extensions.AI.Grok.svg?color=royalblue)](https://www.nuget.org/packages/Devlooped.Extensions.AI.Grok)
[![Downloads](https://img.shields.io/nuget/dt/Devlooped.Extensions.AI.Grok.svg?color=green)](https://www.nuget.org/packages/Devlooped.Extensions.AI.Grok)

<!-- #grok-title -->
Microsoft.Extensions.AI `IChatClient` for Grok with full support for all 
[agentic tools](https://docs.x.ai/docs/guides/tools/overview):

```csharp
var grok = new GrokClient(Environment.GetEnvironmentVariable("XAI_API_KEY")!)
    .AsIChatClient("grok-4.1-fast");
```
<!-- #grok-title -->
<!-- #grok -->
## Web Search

```csharp
var messages = new Chat()
{
    { "system", "You are an AI assistant that knows how to search the web." },
    { "user", "What's Tesla stock worth today? Search X and the news for latest info." },
};

var grok = new GrokClient(Environment.GetEnvironmentVariable("XAI_API_KEY")!).AsIChatClient("grok-4.1-fast");

var options = new ChatOptions
{
    Tools = [new HostedWebSearchTool()] // 👈 compatible with OpenAI
};

var response = await grok.GetResponseAsync(messages, options);
```

In addition to basic web search as shown above, Grok supports more 
[advanced search](https://docs.x.ai/docs/guides/tools/search-tools) scenarios, 
which can be opted-in by using Grok-specific types:

```csharp
var grok = new GrokChatClient(Environment.GetEnvironmentVariable("XAI_API_KEY")!).AsIChatClient("grok-4.1-fast");
var response = await grok.GetResponseAsync(
    "What are the latest product news by Tesla?", 
    new ChatOptions
    {
        Tools = [new GrokSearchTool()
        {
            AllowedDomains = [ "ir.tesla.com" ]
        }]
    });
```

You can alternatively set `ExcludedDomains` instead, and enable image 
understanding with `EnableImageUndestanding`. Learn more about these filters 
at [web search parameters](https://docs.x.ai/docs/guides/tools/search-tools#web-search-parameters).

## X Search

In addition to web search, Grok also supports searching on X (formerly Twitter):

```csharp
var response = await grok.GetResponseAsync(
    "What's the latest on Optimus?", 
    new ChatOptions
    {
        Tools = [new GrokXSearchTool
        {
            // AllowedHandles = [...],
            // ExcludedHandles = [...],
            // EnableImageUnderstanding = true,
            // EnableVideoUnderstanding = true,
            // FromDate = ...,
            // ToDate = ...,
        }]
    });
```

Learn more about available filters at [X search parameters](https://docs.x.ai/docs/guides/tools/search-tools#x-search-parameters).

You can combine both web and X search in the same request by adding both tools.

## Code Execution

The code execution tool enables Grok to write and execute Python code in real-time, 
dramatically expanding its capabilities beyond text generation. This powerful feature 
allows Grok to perform precise calculations, complex data analysis, statistical 
computations, and solve mathematical problems that would be impossible through text alone.

This is Grok's equivalent of the OpenAI code interpreter, and is configured the same way:

```csharp
var grok = new GrokClient(Configuration["XAI_API_KEY"]!).AsIChatClient("grok-4-fast");
var response = await grok.GetResponseAsync(
    "Calculate the compound interest for $10,000 at 5% annually for 10 years",
    new ChatOptions
    {
        Tools = [new HostedCodeInterpreterTool()]
    });

var text = response.Text;
Assert.Contains("$6,288.95", text);
```

If you want to access the output from the code execution, you can add that as an 
include in the options:

```csharp
var grok = new GrokClient(Configuration["XAI_API_KEY"]!).AsIChatClient("grok-4-fast");
var options = new GrokChatOptions
{
    Include = { IncludeOption.CodeExecutionCallOutput },
    Tools = [new HostedCodeInterpreterTool()]
};

var response = await grok.GetResponseAsync(
    "Calculate the compound interest for $10,000 at 5% annually for 10 years",
    options);

var content = response.Messages
    .SelectMany(x => x.Contents)
    .OfType<CodeInterpreterToolResultContent>()
    .First();

foreach (AIContent output in content.Outputs)
    // process outputs from code interpreter
```

Learn more about the [code execution tool](https://docs.x.ai/docs/guides/tools/code-execution-tool).

## Collection Search

If you maintain a [collection](https://docs.x.ai/docs/key-information/collections), 
Grok can perform semantic search on it:

```csharp
var options = new ChatOptions
{
    Tools = [new HostedFileSearchTool {
        Inputs = [new HostedVectorStoreContent("[collection_id]")]
    }]
};
```

Learn more about [collection search](https://docs.x.ai/docs/guides/tools/collections-search-tool).

## Remote MCP

Remote MCP Tools allow Grok to connect to external MCP (Model Context Protocol) servers.
This example sets up the GitHub MCP server so queries about releases (limited specifically 
in this case): 

```csharp
var options = new ChatOptions
{
    Tools = [new HostedMcpServerTool("GitHub", "https://api.githubcopilot.com/mcp/") {
        AuthorizationToken = Configuration["GITHUB_TOKEN"]!,
        AllowedTools = ["list_releases"],
    }]
};
```

Just like with code execution, you can opt-in to surfacing the MCP outputs in 
the response:

```csharp
var options = new GrokChatOptions
{
    // Exposes McpServerToolResultContent in responses
    Include = { IncludeOption.McpCallOutput },
    Tools = [new HostedMcpServerTool("GitHub", "https://api.githubcopilot.com/mcp/") {
        AuthorizationToken = Configuration["GITHUB_TOKEN"]!,
        AllowedTools = ["list_releases"],
    }]
};

```

Learn more about [Remote MCP tools](https://docs.x.ai/docs/guides/tools/remote-mcp-tools).
<!-- #grok -->

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
[![Uno Platform](https://avatars.githubusercontent.com/u/52228309?v=4&s=39 "Uno Platform")](https://github.com/unoplatform)
[![Reuben Swartz](https://avatars.githubusercontent.com/u/724704?u=2076fe336f9f6ad678009f1595cbea434b0c5a41&v=4&s=39 "Reuben Swartz")](https://github.com/rbnswartz)
[![Jacob Foshee](https://avatars.githubusercontent.com/u/480334?v=4&s=39 "Jacob Foshee")](https://github.com/jfoshee)
[![](https://avatars.githubusercontent.com/u/33566379?u=bf62e2b46435a267fa246a64537870fd2449410f&v=4&s=39 "")](https://github.com/Mrxx99)
[![Eric Johnson](https://avatars.githubusercontent.com/u/26369281?u=41b560c2bc493149b32d384b960e0948c78767ab&v=4&s=39 "Eric Johnson")](https://github.com/eajhnsn1)
[![David JENNI](https://avatars.githubusercontent.com/u/3200210?v=4&s=39 "David JENNI")](https://github.com/davidjenni)
[![Jonathan ](https://avatars.githubusercontent.com/u/5510103?u=98dcfbef3f32de629d30f1f418a095bf09e14891&v=4&s=39 "Jonathan ")](https://github.com/Jonathan-Hickey)
[![Ken Bonny](https://avatars.githubusercontent.com/u/6417376?u=569af445b6f387917029ffb5129e9cf9f6f68421&v=4&s=39 "Ken Bonny")](https://github.com/KenBonny)
[![Simon Cropp](https://avatars.githubusercontent.com/u/122666?v=4&s=39 "Simon Cropp")](https://github.com/SimonCropp)
[![agileworks-eu](https://avatars.githubusercontent.com/u/5989304?v=4&s=39 "agileworks-eu")](https://github.com/agileworks-eu)
[![Zheyu Shen](https://avatars.githubusercontent.com/u/4067473?v=4&s=39 "Zheyu Shen")](https://github.com/arsdragonfly)
[![Vezel](https://avatars.githubusercontent.com/u/87844133?v=4&s=39 "Vezel")](https://github.com/vezel-dev)
[![ChilliCream](https://avatars.githubusercontent.com/u/16239022?v=4&s=39 "ChilliCream")](https://github.com/ChilliCream)
[![4OTC](https://avatars.githubusercontent.com/u/68428092?v=4&s=39 "4OTC")](https://github.com/4OTC)
[![Vincent Limo](https://avatars.githubusercontent.com/devlooped-user?s=39 "Vincent Limo")](https://github.com/v-limo)
[![domischell](https://avatars.githubusercontent.com/u/66068846?u=0a5c5e2e7d90f15ea657bc660f175605935c5bea&v=4&s=39 "domischell")](https://github.com/DominicSchell)
[![Adrian Alonso](https://avatars.githubusercontent.com/u/2027083?u=129cf516d99f5cb2fd0f4a0787a069f3446b7522&v=4&s=39 "Adrian Alonso")](https://github.com/adalon)
[![Michael Hagedorn](https://avatars.githubusercontent.com/u/61711586?u=8f653dfcb641e8c18cc5f78692ebc6bb3a0c92be&v=4&s=39 "Michael Hagedorn")](https://github.com/Eule02)
[![](https://avatars.githubusercontent.com/devlooped-user?s=39 "")](https://github.com/henkmartijn)
[![torutek](https://avatars.githubusercontent.com/u/33917059?v=4&s=39 "torutek")](https://github.com/torutek)
[![mccaffers](https://avatars.githubusercontent.com/u/16667079?u=739e110e62a75870c981640447efa5eb2cb3bc8f&v=4&s=39 "mccaffers")](https://github.com/mccaffers)
[![Cleosia](https://avatars.githubusercontent.com/u/85127128?u=3c889baa39bbe3607998c931490bd67c6ed854f2&v=4&s=39 "Cleosia")](https://github.com/cleosia)


<!-- sponsors.md -->
[![Sponsor this project](https://avatars.githubusercontent.com/devlooped-sponsor?s=118 "Sponsor this project")](https://github.com/sponsors/devlooped)

[Learn more about GitHub Sponsors](https://github.com/sponsors)

<!-- https://github.com/devlooped/sponsors/raw/main/footer.md -->
