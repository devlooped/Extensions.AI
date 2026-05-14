---
name: fix-deai001
description: Fix DEAI001 build error: AddChatClients was removed
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
**NEVER remove it** — always convert it to one or more `ConfigureChatClientDefaults` calls (see below).

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

> ⚠️ **The `configure` lambda must NEVER be removed.** Always convert it into the equivalent
> `ConfigureChatClientDefaults` call(s). Dropping the logic would silently change runtime behavior.

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

var grok = app.Services.GetChatClient("AI:Clients:Grok");
var openai = app.Services.GetChatClient("AI:Clients:OpenAI");
```
