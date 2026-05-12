# Agent Notes

- Client providers use `IClientProvider` for configuration-driven creation of chat, speech-to-text, and text-to-speech clients.
- `IClientProvider` and `IClientFactory` are the canonical provider/factory APIs; do not add chat-only compatibility abstractions.
- Resolve section-bound `IClientFactory` instances through `ClientFactoryResolver`; do not register or depend on an unbound singleton `IClientFactory`.
- Provider-created clients should expose the bound provider options through `GetService(typeof(object), "options")` and typed options requests.
- `AddClients` registers both keyed `IClientFactory` (for sections with direct `apikey`) and keyed `IChatClient` (for sections with `modelid`). There is no separate `AddChatClients` — all registration flows through `AddClients`.
- `IChatClient` registrations use `ConfigurableChatClient` for auto-reload; on every configuration reload, the inner client is recreated via the held `IClientFactory`, which applies defaults through `DefaultsApplyingClientFactory` — there is no separate defaults wrapping at the registration level.
- Keyed `IClientFactory` registrations in `AddClients` are `DefaultsApplyingClientFactory(ConfigurationBoundClientFactory(...))` singletons; the `ConfigurationBoundClientFactory` re-resolves the provider on every `Create*Client` call to reflect configuration changes including provider switches — no reload required for factory-created clients.
- `ConfigurableChatClient` holds an `IClientFactory` reference (not `IClientFactoryResolver`); the public resolver-based constructors wrap the resolver in `ConfigurationBoundClientFactory` for backwards compatibility.
