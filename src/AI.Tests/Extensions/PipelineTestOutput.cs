using System.ClientModel.Primitives;
using System.Text.Json;

namespace Devlooped.Extensions.AI;

public static class PipelineTestOutput
{
    static readonly JsonSerializerOptions options = new(JsonSerializerDefaults.General)
    {
        WriteIndented = true,
    };

    public static TOptions WriteTo<TOptions>(this TOptions pipelineOptions, ITestOutputHelper output = default)
        where TOptions : ClientPipelineOptions
    {
        return pipelineOptions.Observe(
            request => output.WriteLine(request.ToJsonString(options)),
            response => output.WriteLine(response.ToJsonString(options))
        );
    }
}