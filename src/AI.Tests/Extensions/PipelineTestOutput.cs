using System.ClientModel.Primitives;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Devlooped.Extensions.AI;

public static class PipelineTestOutput
{
    /// <summary>
    /// Sets a <see cref="ClientPipelineOptions.Transport"/> that renders HTTP messages to the 
    /// console using Spectre.Console rich JSON formatting, but only if the console is interactive.
    /// </summary>
    /// <typeparam name="TOptions">The options type to configure for HTTP logging.</typeparam>
    /// <param name="pipelineOptions">The options instance to configure.</param>
    /// <remarks>
    /// NOTE: this is the lowst-level logging after all chat pipeline processing has been done.
    /// <para>
    /// If the options already provide a transport, it will be wrapped with the console 
    /// logging transport to minimize the impact on existing configurations.
    /// </para>
    /// </remarks>
    public static TOptions UseTestOutput<TOptions>(this TOptions pipelineOptions, ITestOutputHelper output)
        where TOptions : ClientPipelineOptions
    {
        pipelineOptions.Transport = new TestPipelineTransport(pipelineOptions.Transport ?? HttpClientPipelineTransport.Shared, output);

        return pipelineOptions;
    }
}

public class TestPipelineTransport(PipelineTransport inner, ITestOutputHelper? output = null) : PipelineTransport
{
    static readonly JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.General)
    {
        WriteIndented = true,
    };

    public List<JsonNode> Requests { get; } = [];
    public List<JsonNode> Responses { get; } = [];

    protected override async ValueTask ProcessCoreAsync(PipelineMessage message)
    {
        message.BufferResponse = true;
        await inner.ProcessAsync(message);

        if (message.Request.Content is not null)
        {
            using var memory = new MemoryStream();
            message.Request.Content.WriteTo(memory);
            memory.Position = 0;
            using var reader = new StreamReader(memory);
            var content = await reader.ReadToEndAsync();
            var node = JsonNode.Parse(content);
            Requests.Add(node!);
            output?.WriteLine(node!.ToJsonString(options));
        }

        if (message.Response != null)
        {
            var node = JsonNode.Parse(message.Response.Content.ToString());
            Responses.Add(node!);
            output?.WriteLine(node!.ToJsonString(options));
        }
    }

    protected override PipelineMessage CreateMessageCore() => inner.CreateMessage();
    protected override void ProcessCore(PipelineMessage message) => inner.Process(message);
}
