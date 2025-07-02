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
    /// <param name="output">The test output helper to write to.</param>
    /// <param name="onRequest">A callback to process the <see cref="JsonNode"/> that was sent.</param>
    /// <param name="onResponse">A callback to process the <see cref="JsonNode"/> that was received.</param>
    /// <remarks>
    /// NOTE: this is the lowst-level logging after all chat pipeline processing has been done.
    /// <para>
    /// If the options already provide a transport, it will be wrapped with the console 
    /// logging transport to minimize the impact on existing configurations.
    /// </para>
    /// </remarks>
    public static TOptions WriteTo<TOptions>(this TOptions pipelineOptions, ITestOutputHelper? output = default, Action<JsonNode>? onRequest = default, Action<JsonNode>? onResponse = default)
        where TOptions : ClientPipelineOptions
    {
        pipelineOptions.AddPolicy(new TestOutputPolicy(output ?? NullTestOutputHelper.Default, onRequest, onResponse), PipelinePosition.BeforeTransport);
        return pipelineOptions;
    }

    class NullTestOutputHelper : ITestOutputHelper
    {
        public static ITestOutputHelper Default { get; } = new NullTestOutputHelper();
        NullTestOutputHelper() { }
        public void WriteLine(string message) { }
        public void WriteLine(string format, params object[] args) { }
    }

    class TestOutputPolicy(ITestOutputHelper output, Action<JsonNode>? onRequest = default, Action<JsonNode>? onResponse = default) : PipelinePolicy
    {
        static readonly JsonSerializerOptions options = new JsonSerializerOptions(JsonSerializerDefaults.General)
        {
            WriteIndented = true,
        };

        public override void Process(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            message.BufferResponse = true;
            ProcessNext(message, pipeline, currentIndex);

            if (message.Request.Content is not null)
            {
                using var memory = new MemoryStream();
                message.Request.Content.WriteTo(memory);
                memory.Position = 0;
                using var reader = new StreamReader(memory);
                var content = reader.ReadToEnd();
                var node = JsonNode.Parse(content);
                onRequest?.Invoke(node!);
                output?.WriteLine(node!.ToJsonString(options));
            }

            if (message.Response != null)
            {
                var node = JsonNode.Parse(message.Response.Content.ToString());
                onResponse?.Invoke(node!);
                output?.WriteLine(node!.ToJsonString(options));
            }
        }

        public override async ValueTask ProcessAsync(PipelineMessage message, IReadOnlyList<PipelinePolicy> pipeline, int currentIndex)
        {
            message.BufferResponse = true;
            await ProcessNextAsync(message, pipeline, currentIndex);

            if (message.Request.Content is not null)
            {
                using var memory = new MemoryStream();
                message.Request.Content.WriteTo(memory);
                memory.Position = 0;
                using var reader = new StreamReader(memory);
                var content = await reader.ReadToEndAsync();
                var node = JsonNode.Parse(content);
                onRequest?.Invoke(node!);
                output?.WriteLine(node!.ToJsonString(options));
            }

            if (message.Response != null)
            {
                var node = JsonNode.Parse(message.Response.Content.ToString());
                onResponse?.Invoke(node!);
                output?.WriteLine(node!.ToJsonString(options));
            }
        }
    }
}