using System.ClientModel.Primitives;
using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using static ConfigurationExtensions;

namespace Devlooped.Extensions.AI;

public class GrokTests(ITestOutputHelper output)
{
    [SecretsFact("XAI_API_KEY")]
    public async Task GrokInvokesTools()
    {
        var messages = new Chat()
        {
            { "system", "You are a bot that invokes the tool get_date when asked for the date." },
            { "user", "What day is today?" },
        };

        var chat = new GrokChatClient(Configuration["XAI_API_KEY"]!);

        var options = new GrokChatOptions
        {
            ModelId = "grok-3-mini",
            Search = GrokSearch.Auto,
            Tools = [AIFunctionFactory.Create(() => DateTimeOffset.Now.ToString("O"), "get_date")],
            AdditionalProperties = new()
            {
                { "foo", "bar" }
            }
        };

        var response = await chat.GetResponseAsync(messages, options);
        var getdate = response.Messages
            .SelectMany(x => x.Contents.OfType<FunctionCallContent>())
            .Any(x => x.Name == "get_date");

        Assert.True(getdate);
        // NOTE: the chat client was requested as grok-3 but the chat options wanted a 
        // different model and the grok client honors that choice.
        Assert.Equal("grok-3-mini", response.ModelId);
    }

    [SecretsFact("XAI_API_KEY")]
    public async Task GrokInvokesToolAndSearch()
    {
        var messages = new Chat()
        {
            { "system", "You are a bot that invokes the tool 'get_date' before responding to anything since it's important context." },
            { "user", "What's Tesla stock worth today?" },
        };

        var transport = new TestPipelineTransport(HttpClientPipelineTransport.Shared, output);

        var grok = new GrokChatClient(Configuration["XAI_API_KEY"]!, "grok-3", new OpenAI.OpenAIClientOptions() { Transport = transport })
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();

        var options = new GrokChatOptions
        {
            ModelId = "grok-3-mini",
            Search = GrokSearch.On,
            Tools = [AIFunctionFactory.Create(() => DateTimeOffset.Now.ToString("O"), "get_date")]
        };

        var response = await grok.GetResponseAsync(messages, options);

        // assert that the request contains the following node
        // "search_parameters": {
        //      "mode": "on"
        //}
        Assert.All(transport.Requests, x =>
        {
            var search = Assert.IsType<JsonObject>(x["search_parameters"]);
            Assert.Equal("on", search["mode"]?.GetValue<string>());
        });

        // The get_date result shows up as a tool role
        Assert.Contains(response.Messages, x => x.Role == ChatRole.Tool);

        // Citations include nasdaq.com at least as a web search source
        var node = transport.Responses.LastOrDefault();
        Assert.NotNull(node);
        var citations = Assert.IsType<JsonArray>(node["citations"], false);
        var yahoo = citations.Where(x => x != null).Any(x => x!.ToString().Contains("https://finance.yahoo.com/quote/TSLA/", StringComparison.Ordinal));

        Assert.True(yahoo, "Expected at least one citation to nasdaq.com");

        // NOTE: the chat client was requested as grok-3 but the chat options wanted a 
        // different model and the grok client honors that choice.
        Assert.Equal("grok-3-mini", response.ModelId);
    }

    [SecretsFact("XAI_API_KEY")]
    public async Task GrokInvokesHostedSearchTool()
    {
        var messages = new Chat()
        {
            { "system", "You are an AI assistant that knows how to search the web." },
            { "user", "What's Tesla stock worth today? Search X and the news for latest info." },
        };

        var transport = new TestPipelineTransport(HttpClientPipelineTransport.Shared, output);

        var chat = new GrokChatClient(Configuration["XAI_API_KEY"]!, "grok-3", new OpenAI.OpenAIClientOptions() { Transport = transport });

        var options = new ChatOptions
        {
            Tools = [new HostedWebSearchTool()]
        };

        var response = await chat.GetResponseAsync(messages, options);
        var text = response.Text;

        Assert.Contains("TSLA", text);

        // assert that the request contains the following node
        // "search_parameters": {
        //      "mode": "auto"
        //}
        Assert.All(transport.Requests, x =>
        {
            var search = Assert.IsType<JsonObject>(x["search_parameters"]);
            Assert.Equal("auto", search["mode"]?.GetValue<string>());
        });

        // Citations include nasdaq.com at least as a web search source
        Assert.Single(transport.Responses);
        var node = transport.Responses[0];
        Assert.NotNull(node);
        var citations = Assert.IsType<JsonArray>(node["citations"], false);
        var yahoo = citations.Where(x => x != null).Any(x => x!.ToString().Contains("https://finance.yahoo.com/quote/TSLA/", StringComparison.Ordinal));

        Assert.True(yahoo, "Expected at least one citation to nasdaq.com");

        // Uses the default model set by the client when we asked for it
        Assert.Equal("grok-3", response.ModelId);
    }

    [SecretsFact("XAI_API_KEY")]
    public async Task GrokThinksHard()
    {
        var messages = new Chat()
        {
            { "system", "You are an intelligent AI assistant that's an expert on financial matters." },
            { "user", "If you have a debt of 100k and accumulate a compounding 5% debt on top of it every year, how long before you are a negative millonaire?" },
        };

        var grok = new GrokClient(Configuration["XAI_API_KEY"]!)
            .GetChatClient("grok-3")
            .AsIChatClient()
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();

        var options = new GrokChatOptions
        {
            ModelId = "grok-3-mini",
            Search = GrokSearch.Off,
            ReasoningEffort = ReasoningEffort.High,
        };

        var response = await grok.GetResponseAsync(messages, options);

        var text = response.Text;

        Assert.Contains("48 years", text);
        // NOTE: the chat client was requested as grok-3 but the chat options wanted a 
        // different model and the grok client honors that choice.
        Assert.StartsWith("grok-3-mini", response.ModelId);
    }
}
