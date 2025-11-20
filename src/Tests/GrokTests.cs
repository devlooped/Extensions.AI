using System.Text.Json.Nodes;
using Azure;
using Devlooped.Extensions.AI.Grok;
using Microsoft.Extensions.AI;
using OpenAI.Realtime;
using static ConfigurationExtensions;
using OpenAIClientOptions = OpenAI.OpenAIClientOptions;

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

        var chat = new GrokClient(Configuration["XAI_API_KEY"]!).AsIChatClient("grok-4")
            .AsBuilder()
            .UseLogging(output.AsLoggerFactory())
            .Build();

        var options = new GrokChatOptions
        {
            ModelId = "grok-4-fast",
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
        Assert.Equal("grok-4-fast-reasoning", response.ModelId);
    }

    [SecretsFact("XAI_API_KEY")]
    public async Task GrokInvokesToolAndSearch()
    {
        var messages = new Chat()
        {
            { "system", "You are a bot that invokes the tool 'get_date' before responding to anything since it's important context." },
            { "user", "What's Tesla stock worth today?" },
        };

        var requests = new List<JsonNode>();
        var responses = new List<JsonNode>();

        var grok = new GrokChatClient2(Configuration["XAI_API_KEY"]!, "grok-4", OpenAIClientOptions
                    .Observable(requests.Add, responses.Add)
                    .WriteTo(output))
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
        Assert.All(requests, x =>
        {
            var search = Assert.IsType<JsonObject>(x["search_parameters"]);
            Assert.Equal("on", search["mode"]?.GetValue<string>());
        });

        // The get_date result shows up as a tool role
        Assert.Contains(response.Messages, x => x.Role == ChatRole.Tool);

        // Citations include nasdaq.com at least as a web search source
        var node = responses.LastOrDefault();
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

        var requests = new List<JsonNode>();
        var responses = new List<JsonNode>();

        var grok = new GrokChatClient2(Configuration["XAI_API_KEY"]!, "grok-3", OpenAIClientOptions
            .Observable(requests.Add, responses.Add)
            .WriteTo(output));

        var options = new ChatOptions
        {
            Tools = [new HostedWebSearchTool()]
        };

        var response = await grok.GetResponseAsync(messages, options);
        var text = response.Text;

        Assert.Contains("TSLA", text);

        // assert that the request contains the following node
        // "search_parameters": {
        //      "mode": "auto"
        //}
        Assert.All(requests, x =>
        {
            var search = Assert.IsType<JsonObject>(x["search_parameters"]);
            Assert.Equal("auto", search["mode"]?.GetValue<string>());
        });

        // Citations include nasdaq.com at least as a web search source
        Assert.Single(responses);
        var node = responses[0];
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
            { "user", "If you have a debt of 100k and accumulate a compounding 5% debt on top of it every year, how long before you are a negative millonaire? (round up to full integer value)" },
        };

        var grok = new GrokClient2(Configuration["XAI_API_KEY"]!)
            .GetChatClient("grok-3")
            .AsIChatClient();

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

    [SecretsFact("XAI_API_KEY")]
    public async Task GrokInvokesSpecificSearchUrl()
    {
        var messages = new Chat()
        {
            { "system", "Sos un asistente del Cerro Catedral, usas la funcionalidad de Live Search en el sitio oficial." },
            { "system", $"Hoy es {DateTime.Now.ToString("o")}" },
            { "user", "Que calidad de nieve hay hoy?" },
        };

        var requests = new List<JsonNode>();
        var responses = new List<JsonNode>();

        var grok = new GrokChatClient2(Configuration["XAI_API_KEY"]!, "grok-4-fast-non-reasoning", OpenAIClientOptions
            .Observable(requests.Add, responses.Add)
            .WriteTo(output));

        var options = new ChatOptions
        {
            Tools = [new GrokSearchTool(GrokSearch.On)
            {
                //FromDate = new DateOnly(2025, 1, 1),
                //ToDate = DateOnly.FromDateTime(DateTime.Now),
                //MaxSearchResults = 10,
                Sources =
                [
                    new GrokWebSource
                    {
                        AllowedWebsites =
                        [
                            "https://catedralaltapatagonia.com",
                            "https://catedralaltapatagonia.com/parte-de-nieve/",
                            "https://catedralaltapatagonia.com/tarifas/"
                        ]
                    },
                ]
            }]
        };

        var response = await grok.GetResponseAsync(messages, options);
        var text = response.Text;

        // assert that the request contains the following node
        // "search_parameters": {
        //      "mode": "auto"
        //}
        Assert.All(requests, x =>
        {
            var search = Assert.IsType<JsonObject>(x["search_parameters"]);
            Assert.Equal("on", search["mode"]?.GetValue<string>());
        });

        // Citations include catedralaltapatagonia.com at least as a web search source
        Assert.Single(responses);
        var node = responses[0];
        Assert.NotNull(node);
        var citations = Assert.IsType<JsonArray>(node["citations"], false);
        var catedral = citations.Where(x => x != null).Any(x => x!.ToString().Contains("catedralaltapatagonia.com", StringComparison.Ordinal));

        Assert.True(catedral, "Expected at least one citation to catedralaltapatagonia.com");

        // Uses the default model set by the client when we asked for it
        Assert.Equal("grok-4-fast-non-reasoning", response.ModelId);
    }

    [SecretsFact("XAI_API_KEY")]
    public async Task CanAvoidCitations()
    {
        var messages = new Chat()
        {
            { "system", "Sos un asistente del Cerro Catedral, usas la funcionalidad de Live Search en el sitio oficial." },
            { "system", $"Hoy es {DateTime.Now.ToString("o")}" },
            { "user", "Que calidad de nieve hay hoy?" },
        };

        var requests = new List<JsonNode>();
        var responses = new List<JsonNode>();

        var grok = new GrokChatClient2(Configuration["XAI_API_KEY"]!, "grok-4-fast-non-reasoning", OpenAIClientOptions
            .Observable(requests.Add, responses.Add)
            .WriteTo(output));

        var options = new ChatOptions
        {
            Tools = [new GrokSearchTool(GrokSearch.On)
            {
                ReturnCitations = false,
                Sources =
                [
                    new GrokWebSource
                    {
                        AllowedWebsites =
                        [
                            "https://catedralaltapatagonia.com",
                            "https://catedralaltapatagonia.com/parte-de-nieve/",
                            "https://catedralaltapatagonia.com/tarifas/"
                        ]
                    },
                ]
            }]
        };

        var response = await grok.GetResponseAsync(messages, options);
        var text = response.Text;

        // assert that the request contains the following node
        // "search_parameters": {
        //      "mode": "auto"
        //      "return_citations": "false"
        //}
        Assert.All(requests, x =>
        {
            var search = Assert.IsType<JsonObject>(x["search_parameters"]);
            Assert.Equal("on", search["mode"]?.GetValue<string>());
            Assert.False(search["return_citations"]?.GetValue<bool>());
        });

        // Citations are not included
        Assert.Single(responses);
        var node = responses[0];
        Assert.NotNull(node);
        Assert.Null(node["citations"]);
    }

    [SecretsFact("XAI_API_KEY")]
    public async Task GrokGrpcInvokesHostedSearchTool()
    {
        var messages = new Chat()
        {
            { "system", "You are an AI assistant that knows how to search the web." },
            { "user", "What's Tesla stock worth today? Search X and the news for latest info." },
        };

        var grok = new GrokClient(Configuration["XAI_API_KEY"]!).AsIChatClient("grok-4-fast");

        var options = new ChatOptions
        {
            Tools = [new HostedWebSearchTool()]
        };

        var response = await grok.GetResponseAsync(messages, options);
        var text = response.Text;

        Assert.Contains("TSLA", text);
        Assert.NotNull(response.ModelId);
    }

    [SecretsFact("XAI_API_KEY")]
    public async Task GrokGrpcInvokesGrokSearchTool()
    {
        var messages = new Chat()
        {
            { "system", "You are an AI assistant that knows how to search the web." },
            { "user", "What is the latest news about Microsoft?" },
        };

        var grok = new GrokClient(Configuration["XAI_API_KEY"]!).AsIChatClient("grok-4-fast");

        var options = new ChatOptions
        {
            Tools = [new GrokSearchTool 
            { 
                AllowedDomains = ["microsoft.com", "news.microsoft.com"] 
            }]
        };

        var response = await grok.GetResponseAsync(messages, options);
        
        Assert.NotNull(response.Text);
        Assert.Contains("Microsoft", response.Text);
    }
}