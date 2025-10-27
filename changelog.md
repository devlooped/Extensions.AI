# Changelog

## [v0.9.0-rc](https://github.com/devlooped/AI/tree/v0.9.0-rc) (2025-10-27)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.9.0-beta...v0.9.0-rc)

:sparkles: Implemented enhancements:

- Add support for configurable AI context and auto-wiring of tools [\#135](https://github.com/devlooped/AI/pull/135) (@kzu)
- Add support for configurable and composable AI contexts [\#134](https://github.com/devlooped/AI/pull/134) (@kzu)
- Expose configuration metadata from configurable agent/chat [\#130](https://github.com/devlooped/AI/pull/130) (@kzu)
- Allow case-insensitive agents and clients resolution [\#129](https://github.com/devlooped/AI/pull/129) (@kzu)
- Add automatic dedent of description and instructions [\#128](https://github.com/devlooped/AI/pull/128) (@kzu)
- Make sure we always have the AgentId in the ChatResponse [\#127](https://github.com/devlooped/AI/pull/127) (@kzu)

:twisted_rightwards_arrows: Merged:

- Add comprehensive sample with both server and client [\#131](https://github.com/devlooped/AI/pull/131) (@kzu)

## [v0.9.0-beta](https://github.com/devlooped/AI/tree/v0.9.0-beta) (2025-10-17)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.8.3...v0.9.0-beta)

:sparkles: Implemented enhancements:

- Add Grok search option to avoid citations [\#122](https://github.com/devlooped/AI/pull/122) (@kzu)
- Allow setting Grok options via agent configuration too [\#121](https://github.com/devlooped/AI/pull/121) (@kzu)
- Add support for setting reasoning effort and verbosity via config [\#119](https://github.com/devlooped/AI/pull/119) (@kzu)
- Add service-driven ChatMessageStoreFactory for agents [\#118](https://github.com/devlooped/AI/pull/118) (@kzu)
- Add service-driven AIContextProviderFactory for agents [\#117](https://github.com/devlooped/AI/pull/117) (@kzu)
- Initial support for configurable AI agents [\#116](https://github.com/devlooped/AI/pull/116) (@kzu)
- Add configurable support for Azure Inference and OpenAI [\#114](https://github.com/devlooped/AI/pull/114) (@kzu)
- Restructure for upcoming Agents.AI extensions [\#111](https://github.com/devlooped/AI/pull/111) (@kzu)
- Add support for configuration-driven IChatClient registrations [\#108](https://github.com/devlooped/AI/pull/108) (@kzu)
- Simplify X.AI provider name metadata to xai [\#107](https://github.com/devlooped/AI/pull/107) (@kzu)

:bug: Fixed bugs:

- Make sure we provide compatible metadata for OpenAI [\#106](https://github.com/devlooped/AI/pull/106) (@kzu)
- Make sure Grok client provides metadata for telemetry [\#105](https://github.com/devlooped/AI/pull/105) (@kzu)

:twisted_rightwards_arrows: Merged:

- Add docs and samples for both libraries [\#125](https://github.com/devlooped/AI/pull/125) (@kzu)
- Bump all dependencies to latest [\#104](https://github.com/devlooped/AI/pull/104) (@kzu)

## [v0.8.3](https://github.com/devlooped/AI/tree/v0.8.3) (2025-09-10)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.8.2...v0.8.3)

## [v0.8.2](https://github.com/devlooped/AI/tree/v0.8.2) (2025-08-21)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.8.1...v0.8.2)

:sparkles: Implemented enhancements:

- Friendly error when non-matching SDK is used [\#101](https://github.com/devlooped/AI/pull/101) (@kzu)

## [v0.8.1](https://github.com/devlooped/AI/tree/v0.8.1) (2025-08-20)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.8.0...v0.8.1)

:sparkles: Implemented enhancements:

- Add support for Verbosity introduced in GPT-5 [\#100](https://github.com/devlooped/AI/pull/100) (@kzu)

:twisted_rightwards_arrows: Merged:

- Add simple benchmark test for GPT-5 reasoning efforts [\#97](https://github.com/devlooped/AI/pull/97) (@kzu)

## [v0.8.0](https://github.com/devlooped/AI/tree/v0.8.0) (2025-08-08)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.7.4...v0.8.0)

:sparkles: Implemented enhancements:

- Add support for GPT-5 minimal reasoning effort [\#95](https://github.com/devlooped/AI/pull/95) (@kzu)

:bug: Fixed bugs:

- Only emit the ChatClientExtensions if package is referenced [\#93](https://github.com/devlooped/AI/pull/93) (@kzu)

:twisted_rightwards_arrows: Merged:

- Introduce OSMF EULA [\#96](https://github.com/devlooped/AI/pull/96) (@kzu)
- Rename extension so users can tell ours apart [\#92](https://github.com/devlooped/AI/pull/92) (@kzu)

## [v0.7.4](https://github.com/devlooped/AI/tree/v0.7.4) (2025-08-06)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.7.3...v0.7.4)

:sparkles: Implemented enhancements:

- Target net9/10 in package [\#90](https://github.com/devlooped/AI/pull/90) (@kzu)
- Leverage overload resolution attribute to direct compiler [\#89](https://github.com/devlooped/AI/pull/89) (@kzu)

## [v0.7.3](https://github.com/devlooped/AI/tree/v0.7.3) (2025-07-18)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.7.2...v0.7.3)

## [v0.7.2](https://github.com/devlooped/AI/tree/v0.7.2) (2025-07-10)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.7.1...v0.7.2)

:sparkles: Implemented enhancements:

- Allow finding tool calls by the result type only [\#80](https://github.com/devlooped/AI/pull/80) (@kzu)

## [v0.7.1](https://github.com/devlooped/AI/tree/v0.7.1) (2025-07-10)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.7.0...v0.7.1)

:sparkles: Implemented enhancements:

- Sanitize local function names in tools [\#79](https://github.com/devlooped/AI/pull/79) (@kzu)
- Add overload to find calls in a chat response by tool name [\#77](https://github.com/devlooped/AI/pull/77) (@kzu)

## [v0.7.0](https://github.com/devlooped/AI/tree/v0.7.0) (2025-07-07)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.6.2...v0.7.0)

:sparkles: Implemented enhancements:

- Add support for OpenAI web search options and Grok compat [\#74](https://github.com/devlooped/AI/pull/74) (@kzu)
- Enable full Live Search compatibility for Grok [\#72](https://github.com/devlooped/AI/pull/72) (@kzu)

## [v0.6.2](https://github.com/devlooped/AI/tree/v0.6.2) (2025-07-04)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.6.1...v0.6.2)

:sparkles: Implemented enhancements:

- Add tool-focused extension to inspect calls and results [\#71](https://github.com/devlooped/AI/pull/71) (@kzu)
- Add generated JSON context for Chat model objects including additional properties [\#69](https://github.com/devlooped/AI/pull/69) (@kzu)

:twisted_rightwards_arrows: Merged:

- Rename pipeline output helper class [\#70](https://github.com/devlooped/AI/pull/70) (@kzu)

## [v0.6.1](https://github.com/devlooped/AI/tree/v0.6.1) (2025-07-03)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.6.0...v0.6.1)

:sparkles: Implemented enhancements:

- Add public API for observing the client pipeline [\#63](https://github.com/devlooped/AI/pull/63) (@kzu)

:twisted_rightwards_arrows: Merged:

- Remove reference to SponsorLink manifest [\#67](https://github.com/devlooped/AI/pull/67) (@kzu)
- Simplify console HTTP pipeline logging using Observe/Observable [\#66](https://github.com/devlooped/AI/pull/66) (@kzu)

## [v0.6.0](https://github.com/devlooped/AI/tree/v0.6.0) (2025-07-02)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.5.2...v0.6.0)

:sparkles: Implemented enhancements:

- Improve JSON console logging at the client pipeline level [\#60](https://github.com/devlooped/AI/pull/60) (@kzu)
- Create a smarter OpenAI chat client that honors model ID [\#58](https://github.com/devlooped/AI/pull/58) (@kzu)
- Drop ctor overload not receiving a model [\#57](https://github.com/devlooped/AI/pull/57) (@kzu)
- Simplify approach to Grok chat client and dynamic clients [\#56](https://github.com/devlooped/AI/pull/56) (@kzu)

## [v0.5.2](https://github.com/devlooped/AI/tree/v0.5.2) (2025-07-02)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.5.1...v0.5.2)

:sparkles: Implemented enhancements:

- Improve integration with MEAI typical usage pattern [\#53](https://github.com/devlooped/AI/pull/53) (@kzu)

:twisted_rightwards_arrows: Merged:

- Remove projects that belong elsewhere [\#55](https://github.com/devlooped/AI/pull/55) (@kzu)

## [v0.5.1](https://github.com/devlooped/AI/tree/v0.5.1) (2025-07-01)

[Full Changelog](https://github.com/devlooped/AI/compare/v0.5.0...v0.5.1)

## [v0.5.0](https://github.com/devlooped/AI/tree/v0.5.0) (2025-07-01)

[Full Changelog](https://github.com/devlooped/AI/compare/21a457cb50d98c69eda4e62bad971e766f2ec2b6...v0.5.0)

:sparkles: Implemented enhancements:

- Add support for .env files [\#47](https://github.com/devlooped/AI/pull/47) (@kzu)
- Simplify and unify implementation of JSON console logging [\#46](https://github.com/devlooped/AI/pull/46) (@kzu)
- Add opinionated lib for \(mostly?\) single-file agents [\#42](https://github.com/devlooped/AI/pull/42) (@kzu)
- Add first-class support for Grok unique features [\#41](https://github.com/devlooped/AI/pull/41) (@kzu)
- Add more intuitive API for creating chat messages [\#38](https://github.com/devlooped/AI/pull/38) (@kzu)
- Add JSON console output rendering [\#37](https://github.com/devlooped/AI/pull/37) (@kzu)
- Add console JSON logging extension [\#12](https://github.com/devlooped/AI/pull/12) (@kzu)
- Initial CLI with basic command for help/sponsoring [\#9](https://github.com/devlooped/AI/pull/9) (@kzu)
- Add tool-based retrieval tool over the responses API [\#1](https://github.com/devlooped/AI/pull/1) (@kzu)



\* *This Changelog was automatically generated by [github_changelog_generator](https://github.com/github-changelog-generator/github-changelog-generator)*
