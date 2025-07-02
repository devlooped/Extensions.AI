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

IChatClient grok = new GrokClient(Throw.IfNullOrEmpty(Env.Get("XAI_API_KEY")))
    .GetChatClient("grok-3-mini")  // or "grok-3-mini-fast"
    .AsIChatClient();

var options = new GrokChatOptions
{
    ReasoningEffort = ReasoningEffort.High, // or ReasoningEffort.Low
    Search = GrokSearch.Auto,               // or GrokSearch.On or GrokSearch.Off
};

var response = await grok.GetResponseAsync(messages, options);

AnsiConsole.MarkupLine($":robot: {response.Text}");
```

> [!NOTE]
> The most useful namespaces and dependencies for developing Microsoft.Extensions.AI-powered 
> applications are automatically referenced and imported when using this package.

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
<!-- include https://github.com/devlooped/sponsors/raw/main/footer.md -->
<!-- exclude -->