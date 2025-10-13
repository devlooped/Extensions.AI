using System.ClientModel.Primitives;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace Devlooped.Extensions.AI.OpenAI;

static class OpenAIExtensions
{
    public static ChatOptions? SetResponseOptions(this ChatOptions? options)
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
