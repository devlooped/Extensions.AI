using System.ComponentModel;
using Microsoft.Extensions.AI;

namespace Devlooped.Extensions.AI.OpenAI;

/// <summary>
/// Provides OpenAI-specific extension properties for <see cref="ChatOptions"/> when using 
/// the OpenAI Responses API.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class OpenAIExtensions
{
    /// <summary>
    /// Gets or sets the effort level for a reasoning AI model when generating responses.
    /// </summary>
    /// <remarks>
    /// This property is specific to the OpenAI Responses API. Setting this property automatically 
    /// configures the <see cref="ChatOptions.RawRepresentationFactory"/> to properly forward the 
    /// value to the OpenAI endpoint. Do not manually set <see cref="ChatOptions.RawRepresentationFactory"/> 
    /// when using this property.
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown when <see cref="ChatOptions.RawRepresentationFactory"/> 
    /// has been set to a non-OpenAI factory.</exception>
    extension(ChatOptions options)
    {
        /// <summary>Controls how many reasoning tokens the model generates before producing a response.</summary>
        public ReasoningEffort? ReasoningEffort
        {
            get => options.AdditionalProperties?.TryGetValue("reasoning_effort", out var value) == true && value is ReasoningEffort effort ? effort : null;
            set
            {
                if (value is not null)
                {
                    options.AdditionalProperties ??= [];
                    options.AdditionalProperties["reasoning_effort"] = value;
                    EnsureFactory(options);
                }
                else
                {
                    options.AdditionalProperties?.Remove("reasoning_effort");
                }
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="AI.Verbosity"/> level for a GPT-5 model when generating responses.
        /// </summary>
        /// <remarks>
        /// This property is specific to the OpenAI Responses API and only supported by GPT-5+ models. 
        /// Setting this property automatically configures the <see cref="ChatOptions.RawRepresentationFactory"/> 
        /// to properly forward the value to the OpenAI endpoint. Do not manually set 
        /// <see cref="ChatOptions.RawRepresentationFactory"/> when using this property.
        /// </remarks>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="ChatOptions.RawRepresentationFactory"/> 
        /// has been set to a non-OpenAI factory.</exception>
        public Verbosity? Verbosity
        {
            get => options.AdditionalProperties?.TryGetValue("verbosity", out var value) == true && value is Verbosity verbosity ? verbosity : null;
            set
            {
                if (value is not null)
                {
                    options.AdditionalProperties ??= [];
                    options.AdditionalProperties["verbosity"] = value;
                    EnsureFactory(options);
                }
                else
                {
                    options.AdditionalProperties?.Remove("verbosity");
                }
            }
        }
    }

    static void EnsureFactory(ChatOptions options)
    {
        if (options.RawRepresentationFactory is not null &&
            options.RawRepresentationFactory.Target is not ResponseOptionsFactory)
        {
            throw new InvalidOperationException(
                "Cannot use OpenAI Responses API extension properties (ReasoningEffort, Verbosity) when " +
                "RawRepresentationFactory has already been set to a custom factory. These extension " +
                "properties automatically configure the factory for the OpenAI Responses API.");
        }

        options.RawRepresentationFactory ??= new ResponseOptionsFactory(options).CreateResponseCreationOptions;
    }
}