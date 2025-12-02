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
            ModelId = "grok-4-fast-non-reasoning",
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
        Assert.Equal(options.ModelId, response.ModelId);
    }

    [SecretsFact("XAI_API_KEY")]
    public async Task GrokInvokesToolAndSearch()
    {
        var messages = new Chat()
        {
            { "system", "You use Nasdaq for stocks news and prices." },
            { "user", "What's Tesla stock worth today?" },
        };

        var grok = new GrokClient(Configuration["XAI_API_KEY"]!).AsIChatClient("grok-4")
            .AsBuilder()
            .UseLogging(output.AsLoggerFactory())
            .UseFunctionInvocation()
            .Build();

        var getDateCalls = 0;
        var options = new GrokChatOptions
        {
            ModelId = "grok-4-1-fast-non-reasoning",
            Search = GrokSearch.Web,
            Tools = [AIFunctionFactory.Create(() =>
            {
                getDateCalls++;
                return DateTimeOffset.Now.ToString("O");
            }, "get_date", "Gets the current date")],
        };

        var response = await grok.GetResponseAsync(messages, options);

        // The get_date result shows up as a tool role
        Assert.Contains(response.Messages, x => x.Role == ChatRole.Tool);

        // Citations include nasdaq.com at least as a web search source
        var urls = response.Messages
            .SelectMany(x => x.Contents)
            .SelectMany(x => x.Annotations?.OfType<CitationAnnotation>() ?? [])
            .Where(x => x.Url is not null)
            .Select(x => x.Url!)
            .ToList();

        Assert.Equal(1, getDateCalls);
        Assert.Contains(urls, x => x.Host.EndsWith("nasdaq.com"));
        Assert.Contains(urls, x => x.PathAndQuery.Contains("/TSLA"));
        Assert.Equal(options.ModelId, response.ModelId);

        var calls = response.Messages
            .SelectMany(x => x.Contents.OfType<HostedToolCallContent>())
            .ToList();

        Assert.NotEmpty(calls);
        Assert.Contains(calls, x => x.ToolCall.Type == Devlooped.Grok.ToolCallType.WebSearchTool);
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

        var grok = new GrokClient(Configuration["XAI_API_KEY"]!).AsIChatClient("grok-4-1-fast-non-reasoning");

        var options = new ChatOptions
        {
            Tools = [new GrokSearchTool()
            {
                AllowedDomains = [ "catedralaltapatagonia.com" ]
            }]
        };

        var response = await grok.GetResponseAsync(messages, options);
        var text = response.Text;

        var citations = response.Messages
            .SelectMany(x => x.Contents)
            .SelectMany(x => x.Annotations ?? [])
            .OfType<CitationAnnotation>()
            .Where(x => x.Url != null)
            .Select(x => x.Url!.AbsoluteUri)
            .ToList();

        Assert.Contains("https://partediario.catedralaltapatagonia.com/partediario/", citations);
    }

    [SecretsFact("XAI_API_KEY")]
    public async Task GrokInvokesHostedSearchTool()
    {
        var messages = new Chat()
        {
            { "system", "You are an AI assistant that knows how to search the web." },
            { "user", "What's Tesla stock worth today? Search X, Yahoo and the news for latest info." },
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

        var urls = response.Messages
            .SelectMany(x => x.Contents)
            .SelectMany(x => x.Annotations?.OfType<CitationAnnotation>() ?? [])
            .Where(x => x.Url is not null)
            .Select(x => x.Url!)
            .ToList();

        Assert.Contains(urls, x => x.Host == "finance.yahoo.com");
        Assert.Contains(urls, x => x.PathAndQuery.Contains("/TSLA"));
    }

    [SecretsFact("XAI_API_KEY")]
    public async Task GrokInvokesGrokSearchToolIncludesDomain()
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
                AllowedDomains = ["microsoft.com", "news.microsoft.com"],
            }]
        };

        var response = await grok.GetResponseAsync(messages, options);

        Assert.NotNull(response.Text);
        Assert.Contains("Microsoft", response.Text);

        var urls = response.Messages
            .SelectMany(x => x.Contents)
            .SelectMany(x => x.Annotations?.OfType<CitationAnnotation>() ?? [])
            .Where(x => x.Url is not null)
            .Select(x => x.Url!)
            .ToList();

        foreach (var url in urls)
        {
            output.WriteLine(url.ToString());
        }

        Assert.All(urls, x => x.Host.EndsWith(".microsoft.com"));
    }

    [SecretsFact("XAI_API_KEY")]
    public async Task GrokInvokesGrokSearchToolExcludesDomain()
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
                ExcludedDomains = ["blogs.microsoft.com"]
            }]
        };

        var response = await grok.GetResponseAsync(messages, options);

        Assert.NotNull(response.Text);
        Assert.Contains("Microsoft", response.Text);

        var urls = response.Messages
            .SelectMany(x => x.Contents)
            .SelectMany(x => x.Annotations?.OfType<CitationAnnotation>() ?? [])
            .Where(x => x.Url is not null)
            .Select(x => x.Url!)
            .ToList();

        foreach (var url in urls)
        {
            output.WriteLine(url.ToString());
        }

        Assert.DoesNotContain(urls, x => x.Host == "blogs.microsoft.com");
    }

    [SecretsFact("XAI_API_KEY")]
    public async Task GrokInvokesHostedCodeExecution()
    {
        var messages = new Chat()
        {
            { "user", "Calculate the compound interest for $10,000 at 5% annually for 10 years" },
        };

        var grok = new GrokClient(Configuration["XAI_API_KEY"]!).AsIChatClient("grok-4-fast");

        var options = new ChatOptions
        {
            Tools = [new HostedCodeInterpreterTool()]
        };

        var response = await grok.GetResponseAsync(messages, options);
        var text = response.Text;

        Assert.Contains("$6,288.95", text);
        Assert.Contains(
            response.Messages.SelectMany(x => x.Contents).OfType<HostedToolCallContent>(),
            x => x.ToolCall.Type == Devlooped.Grok.ToolCallType.CodeExecutionTool);
    }
}
