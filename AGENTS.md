# Agent Notes

- Client providers use `IClientProvider` for configuration-driven creation of chat, speech-to-text, and text-to-speech clients.
- `IClientProvider` and `IClientFactory` are the canonical provider/factory APIs; do not add chat-only compatibility abstractions.
- Provider-created clients should expose the bound provider options through `GetService(typeof(object), "options")` and typed options requests.
