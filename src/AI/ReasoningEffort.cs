namespace Devlooped.Extensions.AI;

/// <summary>
/// Effort a reasoning model should apply when generating a response.
/// </summary>
public enum ReasoningEffort
{
    /// <summary>
    /// Minimal reasoning effort, which may result in faster responses. Support depends on the model.
    /// </summary>
    Minimal,
    /// <summary>
    /// Low effort reasoning, which may result in faster responses but less detailed or accurate answers.
    /// </summary>
    Low,
    /// <summary>
    /// Grok in particular does not support this mode, so it will default to <see cref="Low"/>.
    /// </summary>
    Medium,
    /// <summary>
    /// High effort reasoning, which may take longer but provides more detailed and accurate responses.
    /// </summary>
    High
}