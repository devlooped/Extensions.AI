using System.ClientModel.Primitives;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace Devlooped.Extensions.AI.OpenAI;

/// <summary>
/// Allows applying extension properties to the <see cref="ChatOptions"/> when using 
/// them with an OpenAI client.
/// </summary>
public static class OpenAIExtensions
{
    /// <summary>
    /// Applies the extension properties to the <paramref name="options"/> so that 
    /// the underlying OpenAI client can properly forward them to the endpoint.
    /// </summary>
    /// <remarks>
    /// Only use this if you are not using <see cref="OpenAIChatClient"/>, which already applies 
    /// extensions before sending requests.
    /// </remarks>
    /// <returns>An options with the right <see cref="ChatOptions.RawRepresentationFactory"/> replaced 
    /// so it can forward extensions to the underlying OpenAI API.</returns>
    public static ChatOptions? ApplyExtensions(this ChatOptions? options)
    {
        if (options is null)
            return null;

        if (options.ReasoningEffort.HasValue || options.Verbosity.HasValue)
        {
            options.RawRepresentationFactory = _ =>
            {
                var creation = new ResponseCreationOptions();
                if (options.ReasoningEffort.HasValue)
                    creation.ReasoningOptions = new ReasoningEffortOptions(options.ReasoningEffort!.Value);

                if (options.Verbosity.HasValue)
                    creation.TextOptions = new VerbosityOptions(options.Verbosity!.Value);

                return creation;
            };
        }

        return options;
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
