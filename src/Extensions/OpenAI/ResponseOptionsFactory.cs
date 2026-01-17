using System.ClientModel.Primitives;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace Devlooped.Extensions.AI.OpenAI;

class ResponseOptionsFactory(ChatOptions options)
{
    public ResponseCreationOptions CreateResponseCreationOptions(IChatClient client)
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
