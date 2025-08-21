# Changelog

## [v0.8.2](https://github.com/devlooped/Extensions.AI/tree/v0.8.2) (2025-08-21)

[Full Changelog](https://github.com/devlooped/Extensions.AI/compare/v0.8.1...v0.8.2)

:sparkles: Implemented enhancements:

- Friendly error when non-matching SDK is used [\#101](https://github.com/devlooped/Extensions.AI/pull/101) (@kzu)

## [v0.8.1](https://github.com/devlooped/Extensions.AI/tree/v0.8.1) (2025-08-20)

[Full Changelog](https://github.com/devlooped/Extensions.AI/compare/v0.8.0...v0.8.1)

:sparkles: Implemented enhancements:

- Add support for Verbosity introduced in GPT-5 [\#100](https://github.com/devlooped/Extensions.AI/pull/100) (@kzu)

:twisted_rightwards_arrows: Merged:

- Add simple benchmark test for GPT-5 reasoning efforts [\#97](https://github.com/devlooped/Extensions.AI/pull/97) (@kzu)

## [v0.8.0](https://github.com/devlooped/Extensions.AI/tree/v0.8.0) (2025-08-08)

[Full Changelog](https://github.com/devlooped/Extensions.AI/compare/v0.7.4...v0.8.0)

:sparkles: Implemented enhancements:

- Add support for GPT-5 minimal reasoning effort [\#95](https://github.com/devlooped/Extensions.AI/pull/95) (@kzu)

:bug: Fixed bugs:

- Only emit the ChatClientExtensions if package is referenced [\#93](https://github.com/devlooped/Extensions.AI/pull/93) (@kzu)

:twisted_rightwards_arrows: Merged:

- Introduce OSMF EULA [\#96](https://github.com/devlooped/Extensions.AI/pull/96) (@kzu)
- Rename extension so users can tell ours apart [\#92](https://github.com/devlooped/Extensions.AI/pull/92) (@kzu)

## [v0.7.4](https://github.com/devlooped/Extensions.AI/tree/v0.7.4) (2025-08-06)

[Full Changelog](https://github.com/devlooped/Extensions.AI/compare/v0.7.3...v0.7.4)

:sparkles: Implemented enhancements:

- Target net9/10 in package [\#90](https://github.com/devlooped/Extensions.AI/pull/90) (@kzu)
- Leverage overload resolution attribute to direct compiler [\#89](https://github.com/devlooped/Extensions.AI/pull/89) (@kzu)

## [v0.7.3](https://github.com/devlooped/Extensions.AI/tree/v0.7.3) (2025-07-18)

[Full Changelog](https://github.com/devlooped/Extensions.AI/compare/v0.7.2...v0.7.3)

## [v0.7.2](https://github.com/devlooped/Extensions.AI/tree/v0.7.2) (2025-07-10)

[Full Changelog](https://github.com/devlooped/Extensions.AI/compare/v0.7.1...v0.7.2)

:sparkles: Implemented enhancements:

- Allow finding tool calls by the result type only [\#80](https://github.com/devlooped/Extensions.AI/pull/80) (@kzu)

## [v0.7.1](https://github.com/devlooped/Extensions.AI/tree/v0.7.1) (2025-07-10)

[Full Changelog](https://github.com/devlooped/Extensions.AI/compare/v0.7.0...v0.7.1)

:sparkles: Implemented enhancements:

- Sanitize local function names in tools [\#79](https://github.com/devlooped/Extensions.AI/pull/79) (@kzu)
- Add overload to find calls in a chat response by tool name [\#77](https://github.com/devlooped/Extensions.AI/pull/77) (@kzu)

## [v0.7.0](https://github.com/devlooped/Extensions.AI/tree/v0.7.0) (2025-07-07)

[Full Changelog](https://github.com/devlooped/Extensions.AI/compare/v0.6.2...v0.7.0)

:sparkles: Implemented enhancements:

- Add support for OpenAI web search options and Grok compat [\#74](https://github.com/devlooped/Extensions.AI/pull/74) (@kzu)
- Enable full Live Search compatibility for Grok [\#72](https://github.com/devlooped/Extensions.AI/pull/72) (@kzu)

## [v0.6.2](https://github.com/devlooped/Extensions.AI/tree/v0.6.2) (2025-07-04)

[Full Changelog](https://github.com/devlooped/Extensions.AI/compare/v0.6.1...v0.6.2)

:sparkles: Implemented enhancements:

- Add tool-focused extension to inspect calls and results [\#71](https://github.com/devlooped/Extensions.AI/pull/71) (@kzu)
- Add generated JSON context for Chat model objects including additional properties [\#69](https://github.com/devlooped/Extensions.AI/pull/69) (@kzu)

:twisted_rightwards_arrows: Merged:

- Rename pipeline output helper class [\#70](https://github.com/devlooped/Extensions.AI/pull/70) (@kzu)

## [v0.6.1](https://github.com/devlooped/Extensions.AI/tree/v0.6.1) (2025-07-03)

[Full Changelog](https://github.com/devlooped/Extensions.AI/compare/v0.6.0...v0.6.1)

:sparkles: Implemented enhancements:

- Add public API for observing the client pipeline [\#63](https://github.com/devlooped/Extensions.AI/pull/63) (@kzu)

:twisted_rightwards_arrows: Merged:

- Remove reference to SponsorLink manifest [\#67](https://github.com/devlooped/Extensions.AI/pull/67) (@kzu)
- Simplify console HTTP pipeline logging using Observe/Observable [\#66](https://github.com/devlooped/Extensions.AI/pull/66) (@kzu)

## [v0.6.0](https://github.com/devlooped/Extensions.AI/tree/v0.6.0) (2025-07-02)

[Full Changelog](https://github.com/devlooped/Extensions.AI/compare/v0.5.2...v0.6.0)

:sparkles: Implemented enhancements:

- Improve JSON console logging at the client pipeline level [\#60](https://github.com/devlooped/Extensions.AI/pull/60) (@kzu)
- Create a smarter OpenAI chat client that honors model ID [\#58](https://github.com/devlooped/Extensions.AI/pull/58) (@kzu)
- Drop ctor overload not receiving a model [\#57](https://github.com/devlooped/Extensions.AI/pull/57) (@kzu)
- Simplify approach to Grok chat client and dynamic clients [\#56](https://github.com/devlooped/Extensions.AI/pull/56) (@kzu)

## [v0.5.2](https://github.com/devlooped/Extensions.AI/tree/v0.5.2) (2025-07-02)

[Full Changelog](https://github.com/devlooped/Extensions.AI/compare/v0.5.1...v0.5.2)

:sparkles: Implemented enhancements:

- Improve integration with MEAI typical usage pattern [\#53](https://github.com/devlooped/Extensions.AI/pull/53) (@kzu)

:twisted_rightwards_arrows: Merged:

- Remove projects that belong elsewhere [\#55](https://github.com/devlooped/Extensions.AI/pull/55) (@kzu)

## [v0.5.1](https://github.com/devlooped/Extensions.AI/tree/v0.5.1) (2025-07-01)

[Full Changelog](https://github.com/devlooped/Extensions.AI/compare/v0.5.0...v0.5.1)

## [v0.5.0](https://github.com/devlooped/Extensions.AI/tree/v0.5.0) (2025-07-01)

[Full Changelog](https://github.com/devlooped/Extensions.AI/compare/21a457cb50d98c69eda4e62bad971e766f2ec2b6...v0.5.0)

:sparkles: Implemented enhancements:

- Add support for .env files [\#47](https://github.com/devlooped/Extensions.AI/pull/47) (@kzu)
- Simplify and unify implementation of JSON console logging [\#46](https://github.com/devlooped/Extensions.AI/pull/46) (@kzu)
- Add opinionated lib for \(mostly?\) single-file agents [\#42](https://github.com/devlooped/Extensions.AI/pull/42) (@kzu)
- Add first-class support for Grok unique features [\#41](https://github.com/devlooped/Extensions.AI/pull/41) (@kzu)
- Add more intuitive API for creating chat messages [\#38](https://github.com/devlooped/Extensions.AI/pull/38) (@kzu)
- Add JSON console output rendering [\#37](https://github.com/devlooped/Extensions.AI/pull/37) (@kzu)
- Add console JSON logging extension [\#12](https://github.com/devlooped/Extensions.AI/pull/12) (@kzu)
- Initial CLI with basic command for help/sponsoring [\#9](https://github.com/devlooped/Extensions.AI/pull/9) (@kzu)
- Add tool-based retrieval tool over the responses API [\#1](https://github.com/devlooped/Extensions.AI/pull/1) (@kzu)



\* *This Changelog was automatically generated by [github_changelog_generator](https://github.com/github-changelog-generator/github-changelog-generator)*
