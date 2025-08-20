namespace Devlooped.Extensions.AI;

/// <summary>
/// Verbosity determines how many output tokens are generated for models that support it, such as GPT-5.
/// Lowering the number of tokens reduces overall latency.
/// </summary>
/// <see cref="https://platform.openai.com/docs/guides/latest-model?utm_source=chatgpt.com#verbosity"/>
public enum Verbosity
{
    Low,
    Medium,
    High
}
