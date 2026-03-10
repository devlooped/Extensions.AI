using System.ClientModel.Primitives;
using System.Text.Json;
using Microsoft.Extensions.AI;
using OpenAI.Responses;

namespace Devlooped.Extensions.AI.OpenAI;

class ResponseOptionsFactory(ChatOptions options)
{
    public CreateResponseOptions NewCreateResponseOptions(IChatClient client)
    {
        var creation = new CreateResponseOptions();

        if (options.Verbosity is { } verbosity)
            creation.TextOptions = new VerbosityOptions(verbosity);

        return creation;
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
