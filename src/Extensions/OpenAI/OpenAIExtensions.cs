using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace Devlooped.Extensions.AI.OpenAI;

/// <summary>
/// Provides OpenAI-specific extension properties for <see cref="ChatOptions"/> when using 
/// the OpenAI Responses API.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class OpenAIExtensions
{
    // Key used to mark that our factory has been installed
    const string FactoryInstalledKey = "__OpenAIResponsesFactory";

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
        public ReasoningEffort? ReasoningEffort
        {
            get => options.AdditionalProperties?.TryGetValue("reasoning_effort", out var value) == true && value is ReasoningEffort effort ? effort : null;
            set
            {
                if (value is not null)
                {
                    options.AdditionalProperties ??= [];
                    options.AdditionalProperties["reasoning_effort"] = value;
                    EnsureFactoryInstalled(options);
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
                    EnsureFactoryInstalled(options);
                }
                else
                {
                    options.AdditionalProperties?.Remove("verbosity");
                }
            }
        }
    }

    static void EnsureFactoryInstalled(ChatOptions options)
    {
        options.AdditionalProperties ??= [];

        // Check if our factory is already installed
        if (options.AdditionalProperties.TryGetValue(FactoryInstalledKey, out var _))
            return;

        // Check if a different factory has been set
        if (options.RawRepresentationFactory is not null)
        {
            throw new InvalidOperationException(
                "Cannot use OpenAI Responses API extension properties (ReasoningEffort, Verbosity) when " +
                "RawRepresentationFactory has already been set to a custom factory. These extension " +
                "properties automatically configure the factory for the OpenAI Responses API.");
        }

        // Install our factory
        options.RawRepresentationFactory = _ => CreateResponseCreationOptions(options);
        options.AdditionalProperties[FactoryInstalledKey] = true;
    }

    static ResponseCreationOptions CreateResponseCreationOptions(ChatOptions options)
    {
        var creation = new ResponseCreationOptions();

        if (options.ReasoningEffort is { } effort)
            creation.ReasoningOptions = new ReasoningEffortOptions(effort);

        if (options.Verbosity is { } verbosity)
            creation.TextOptions = new VerbosityOptions(verbosity);

        return creation;
    }

    class ReasoningEffortOptions(ReasoningEffort effort) : ResponseReasoningOptions
    {
        protected override void JsonModelWriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions options)
        {
            writer.WritePropertyName("effort"u8);
            writer.WriteStringValue(effort.ToString().ToLowerInvariant());
            base.JsonModelWriteCore(writer, options);
        }
    }

    class VerbosityOptions(Verbosity verbosity) : ResponseTextOptions
    {
        protected override void JsonModelWriteCore(Utf8JsonWriter writer, ModelReaderWriterOptions options)
        {
            writer.WritePropertyName("verbosity"u8);
            writer.WriteStringValue(verbosity.ToString().ToLowerInvariant());
            base.JsonModelWriteCore(writer, options);
        }
    }
}

/// <summary>
/// Extended <see cref="ChatOptions"/> that includes OpenAI Responses API specific properties.
/// </summary>
/// <remarks>
/// This class is provided for configuration binding scenarios. The <see cref="ReasoningEffort"/> 
/// and <see cref="Verbosity"/> properties are specific to the OpenAI Responses API.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
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
