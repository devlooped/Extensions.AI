using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI.OpenAI;

/// <summary>
/// Extended <see cref="ChatOptions"/> that includes OpenAI Responses API specific properties.
/// </summary>
/// <remarks>
/// This class is provided for configuration binding scenarios. The <see cref="ReasoningEffort"/> 
/// and <see cref="Verbosity"/> properties are specific to the OpenAI Responses API.
/// </remarks>
public class OpenAIChatOptions : ChatOptions
{
    /// <summary>
    /// Gets or sets the effort level for a reasoning AI model when generating responses.
    /// </summary>
    /// <remarks>
    /// This property is specific to the OpenAI Responses API.
    /// </remarks>
    public ReasoningEffort? ReasoningEffort
    {
        get => ((ChatOptions)this).ReasoningEffort;
        set => ((ChatOptions)this).ReasoningEffort = value;
    }

    /// <summary>
    /// Gets or sets the verbosity level for a GPT-5+ model when generating responses.
    /// </summary>
    /// <remarks>
    /// This property is specific to the OpenAI Responses API and only supported by GPT-5+ models.
    /// </remarks>
    public Verbosity? Verbosity
    {
        get => ((ChatOptions)this).Verbosity;
        set => ((ChatOptions)this).Verbosity = value;
    }
}
