---
name: devlooped.extensions.ai
description: >
  Helps use and configure Devlooped.Extensions.AI — configuration-driven AI client registration for
  Microsoft.Extensions.AI. Use this skill when working with AddAIClients, ConfigureChatClientDefaults,
  IClientFactory, IClientProvider, ConfigurableChatClient, DEAI001 migration, or keyed IChatClient
  registrations in a project that references Devlooped.Extensions.AI.
---

# Devlooped.Extensions.AI

`Devlooped.Extensions.AI` provides configuration-driven, auto-reloading AI client registration for
`Microsoft.Extensions.AI`. Clients are resolved from `appsettings.json` (or any `IConfiguration`
source) using provider detection, and support hot-reload without restarting the application.

## DEAI001 — Migrate from AddChatClients to AddAIClients

`AddChatClients` is **removed** (error, `DiagnosticId = "DEAI001"`). Replace every call site:

### Before (error)

```csharp
// IHostApplicationBuilder overload
builder.AddChatClients();
builder.AddChatClients(configure: (name, b) => b.UseLogging().UseOpenTelemetry());
builder.AddChatClients(prefix: "ai:clients", useDefaultProviders: true);

// IServiceCollection overload
services.AddChatClients(configuration);
services.AddChatClients(configuration, configure: (name, b) => b.UseLogging());
```

### After (new API)

```csharp
// IHostApplicationBuilder — simplest form
builder.AddAIClients();

// With custom prefix or provider control
builder.AddAIClients(prefix: "ai:clients", useDefaultProviders: true);

// IServiceCollection overload
services.AddAIClients(configuration);
services.AddAIClients(configuration, prefix: "ai:clients", useDefaultProviders: true);
```

The `configure: (name, builder) => ...` callback has **no direct equivalent** in `AddAIClients`.
Move that logic to `ConfigureChatClientDefaults` (see below).

## ConfigureChatClientDefaults

Replaces the inline `configure` callback from the old `AddChatClients`. Separate from
`AddAIClients` so defaults can be declared independently and accumulated.

### Global defaults (all clients of that modality)

```csharp
// Apply to every IChatClient registered via AddAIClients
builder.ConfigureChatClientDefaults(b => b
    .UseLogging()
    .UseOpenTelemetry());

// Speech clients
builder.ConfigureTextToSpeechClientDefaults(b => b.UseLogging());
builder.ConfigureSpeechToTextClientDefaults(b => b.UseLogging());

// IServiceCollection form
services.ConfigureChatClientDefaults(b => b.UseLogging());
```

### Section-specific defaults (one configuration section only)

The section path must use `:` as separator (not `.`), and is matched case-insensitively.
No parent-section inheritance — it matches the exact section path only.

```csharp
// Only the client from "AI:Clients:Grok" gets this pipeline
builder.ConfigureChatClientDefaults("AI:Clients:Grok", b => b.UseRateLimiting());

// IServiceCollection form
services.ConfigureChatClientDefaults("AI:Clients:Grok", b => b.UseRateLimiting());
```

### Migration: configure callback → ConfigureChatClientDefaults

```csharp
// Before
builder.AddChatClients(configure: (name, b) =>
{
    b.UseLogging();
    if (name == "AI:Clients:Grok")
        b.UseRateLimiting();
});

// After
builder
    .ConfigureChatClientDefaults(b => b.UseLogging())
    .ConfigureChatClientDefaults("AI:Clients:Grok", b => b.UseRateLimiting())
    .AddAIClients();
```

Multiple calls accumulate in registration order. Global and section-specific registrations can be
freely mixed. Call order (global then section-specific then global again, etc.) is preserved.

## Configuration Structure

```json
{
  "AI": {
    "Clients": {
      "OpenAI": {
        "ApiKey": "sk-...",
        "ModelId": "gpt-4o"
      },
      "Grok": {
        "Endpoint": "https://api.x.ai/v1",
        "ApiKey": "xai-...",
        "ModelId": "grok-3-fast"
      },
      "AzureOpenAI": {
        "Endpoint": "https://my-resource.openai.azure.com/",
        "ApiKey": "...",
        "ModelId": "gpt-4o"
      }
    }
  }
}
```

- Sections with `apikey` get a keyed `IClientFactory` registered.
- Sections with `modelid` get a keyed `IChatClient` registered.
- Section keys become the service keys (e.g. `"Grok"`, `"AI:Clients:Grok"`, `"AI.Clients.Grok"`).

## Resolving Registered Clients

```csharp
// IChatClient — keyed by client name (short form) or full section path
var chat = app.Services.GetRequiredKeyedService<IChatClient>("Grok");

// Convenience extension (case-insensitive, short or full key)
var chat = app.Services.GetChatClient("Grok");

// IClientFactory — keyed by full section path (or dotted equivalent)
var factory = app.Services.GetRequiredKeyedService<IClientFactory>("AI:Clients:OpenAI");
var chatClient      = factory.CreateChatClient();
var speechToText    = factory.CreateSpeechToTextClient();
var textToSpeech    = factory.CreateTextToSpeechClient();
```

## Custom Providers

Register a custom `IClientProvider` to support additional AI backends:

```csharp
// By type
services.AddAIClientProvider<MyCustomProvider>();

// By factory
services.AddAIClientProvider(sp => new MyCustomProvider(sp.GetRequiredService<IOptions<MyOptions>>().Value));
```

`IClientProvider` controls how the provider is detected from configuration and how client factories
are created. Implement `ProviderName`, `BaseUri`, `HostSuffix`, and `GetFactory(IConfigurationSection)`.

## Auto-Reload

`IChatClient` registrations created by `AddAIClients` use `ConfigurableChatClient`, which rebuilds
the inner client on every `IConfiguration` reload. Change `appsettings.json` and the next call uses
the updated model, endpoint, or API key — no restart needed.

`IClientFactory` registrations use `ConfigurableClientFactory`, which re-resolves the provider on
every `Create*Client()` call, also picking up configuration changes automatically.

## Full Registration Example

```csharp
var builder = new HostApplicationBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder
    .ConfigureChatClientDefaults(b => b           // global: all chat clients
        .UseLogging()
        .UseOpenTelemetry())
    .ConfigureChatClientDefaults("AI:Clients:Grok", b => b  // section-specific
        .UseRateLimiting())
    .AddAIClients();                              // register from configuration

var app = builder.Build();

var grok = app.Services.GetChatClient("Grok");
var openai = app.Services.GetChatClient("OpenAI");
```
