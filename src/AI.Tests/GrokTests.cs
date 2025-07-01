namespace Devlooped.Extensions.AI;

using Microsoft.Extensions.AI;
using static ConfigurationExtensions;

public class GrokTests
{
    [SecretsFact("XAI_API_KEY")]
    public async Task GrokInvokesTools()
    {
        var messages = new Chat()
        {
            { "system", "You are a bot that invokes the tool get_date when asked for the date." },
            { "user", "What day is today?" },
        };

        var grok = new GrokClient(Configuration["XAI_API_KEY"]!);

        var options = new GrokChatOptions
        {
            ModelId = "grok-3-mini",
            Search = GrokSearch.Auto,
            Tools = [AIFunctionFactory.Create(() => DateTimeOffset.Now.ToString("O"), "get_date")]
        };

        var response = await grok.GetResponseAsync(messages, options);
        var getdate = response.Messages
            .SelectMany(x => x.Contents.OfType<FunctionCallContent>())
            .Any(x => x.Name == "get_date");

        Assert.True(getdate);
    }

    [SecretsFact("XAI_API_KEY")]
    public async Task GrokInvokesToolAndSearch()
    {
        var messages = new Chat()
        {
            { "system", "You are a bot that invokes the tool 'get_date' before responding to anything since it's important context." },
            { "user", "What's Tesla stock worth today?" },
        };

        var grok = new GrokClient(Configuration["XAI_API_KEY"]!)
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

        // The get_date result shows up as a tool role
        Assert.Contains(response.Messages, x => x.Role == ChatRole.Tool);

        var text = response.Text;

        Assert.Contains("TSLA", text);
        Assert.Contains("$", text);
        Assert.Contains("Nasdaq", text, StringComparison.OrdinalIgnoreCase);
    }

    [SecretsFact("XAI_API_KEY")]
    public async Task GrokInvokesHostedSearchTool()
    {
        var messages = new Chat()
        {
            { "system", "You are an AI assistant that knows how to search the web." },
            { "user", "What's Tesla stock worth today? Search X and the news for latest info." },
        };

        var grok = new GrokClient(Configuration["XAI_API_KEY"]!);

        var options = new ChatOptions
        {
            ModelId = "grok-3",
            Tools = [new HostedWebSearchTool()]
        };

        var response = await grok.GetResponseAsync(messages, options);
        var text = response.Text;

        Assert.Contains("TSLA", text);
        Assert.Contains("$", text);
        Assert.Contains("Nasdaq", text, StringComparison.OrdinalIgnoreCase);
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
    }
}
