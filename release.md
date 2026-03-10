## vNext Release Notes

#### Breaking Changes

#### Namespace Relocations

Several types have been moved from `Devlooped.Extensions.AI` to `Devlooped.Extensions.AI.OpenAI`:

| Type | Old Namespace | New Namespace |
|------|---------------|---------------|
| `Verbosity` | `Devlooped.Extensions.AI` | `Devlooped.Extensions.AI.OpenAI` |
| `WebSearchTool` | `Devlooped.Extensions.AI` | `Devlooped.Extensions.AI.OpenAI` |

**Migration:** Update your `using` statements from `Devlooped.Extensions.AI` to `Devlooped.Extensions.AI.OpenAI` for these types.

#### Removed Types

The following types have been **removed entirely**:

| Type | Replacement |
|------|-------------|
| `AzureInferenceChatClient` | Use the built-in `IChatClientProvider` infrastructure with `AddChatClientProvider` |
| `AzureOpenAIChatClient` | Use the built-in `IChatClientProvider` infrastructure with `AddChatClientProvider` |
| `OpenAIChatClient` | Use the built-in `IChatClientProvider` infrastructure with `AddChatClientProvider` |
| `OpenAIWebSearchToolExtensions` | Properties (`City`, `Region`, `TimeZone`, `ContextSize`) are now directly on `WebSearchTool` |
| `ReasoningEffort` | Use `ChatOptions.ReasoningEffort` from `Microsoft.Extensions.AI` (built-in string-based property) |

#### WebSearchTool Changes

The `WebSearchTool` class has been significantly simplified:

- **Constructor:** Now accepts an optional `country` parameter (`string? country = null`) instead of a required one
- **Properties moved inline:** `City`, `Region`, and `TimeZone` are now properties directly on `WebSearchTool` (no longer extension properties)
- **New property:** `AllowedDomains` (`string[]?`) has been added
- **Removed:** The `ContextSize` extension property has been removed as it's no longer documented on OpenAI's official documentation

#### OpenAIChatOptions Changes

- The `ReasoningEffort` property has been removed from `OpenAIChatOptions`. Use the base `ChatOptions.ReasoningEffort` property directly instead (available in `Microsoft.Extensions.AI`).

#### ConfigurableChatClient Changes

- The `Options` property has been removed from `ConfigurableChatClient` since the new provider-based architecture does not require it
- A new constructor signature is now available with explicit parameters

#### Method Signature Changes

##### AddChatClients

The `AddChatClients` extension methods now include an additional optional parameter:

```csharp
// Old signature
AddChatClients(services, configuration, configurePipeline, configureClient, prefix);

// New signature  
AddChatClients(services, configuration, configurePipeline, configureClient, prefix, useDefaultProviders: true);
```

The overload allows skipping the registration of default providers when set to `false`.

#### Removed Methods

- `OpenAIExtensions.ApplyExtensions(ChatOptions?)` has been removed since the new OpenAI-specific ChatOptions use a new mechanism based on lazy initialization of the `ChatOptions.RawRepresentationFactory` to apply the values.
- `ChatOptions.ReasoningEffort` extension property (from `OpenAIExtensions`) has been removed. The `ReasoningEffort` concept is now natively supported by the base `ChatOptions.ReasoningEffort` property in `Microsoft.Extensions.AI`.

---

### Migration Guide

1. **Update namespace imports** for `Verbosity` and `WebSearchTool` to use `Devlooped.Extensions.AI.OpenAI`

2. **Replace `ReasoningEffort` enum** with the built-in `ChatOptions.ReasoningEffort` string property from `Microsoft.Extensions.AI`. For example, use `options.ReasoningEffort = "medium"` instead of `options.ReasoningEffort = ReasoningEffort.Medium`.

3. **Replace custom chat clients** (`AzureInferenceChatClient`, `AzureOpenAIChatClient`, `OpenAIChatClient`) with the new provider-based architecture using `IChatClientProvider` and `AddChatClientProvider`

4. **WebSearchTool usage** is source-compatible once you import the correct OpenAI namespace.

5. **Use `OpenAIChatOptions`** for typed and binding-friendly configuration of OpenAI-specific options.
