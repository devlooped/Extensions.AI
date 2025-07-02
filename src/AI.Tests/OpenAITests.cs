using System.Text.Json.Nodes;
using Microsoft.Extensions.AI;
using static ConfigurationExtensions;

namespace Devlooped.Extensions.AI;

public class OpenAITests(ITestOutputHelper output)
{
    [SecretsFact("OPENAI_API_KEY")]
    public async Task OpenAISwitchesModel()
    {
        var messages = new Chat()
        {
            { "user", "What products does Tesla make?" },
        };

        var chat = new OpenAIChatClient(Configuration["OPENAI_API_KEY"]!, "gpt-4.1-nano", new OpenAI.OpenAIClientOptions().WriteTo(output));

        var options = new ChatOptions
        {
            ModelId = "gpt-4.1-mini",
        };

        var response = await chat.GetResponseAsync(messages, options);

        // NOTE: the chat client was requested as grok-3 but the chat options wanted a 
        // different model and the grok client honors that choice.
        Assert.StartsWith("gpt-4.1-mini", response.ModelId);
    }

    [SecretsFact("OPENAI_API_KEY")]
    public async Task OpenAIThinks()
    {
        var messages = new Chat()
        {
            { "system", "You are an intelligent AI assistant that's an expert on financial matters." },
            { "user", "If you have a debt of 100k and accumulate a compounding 5% debt on top of it every year, how long before you are a negative millonaire? (round up to full integer value)" },
        };

        var requests = new List<JsonNode>();

        var chat = new OpenAIChatClient(Configuration["OPENAI_API_KEY"]!, "o3-mini", new OpenAI.OpenAIClientOptions()
            .WriteTo(output, requests.Add));

        var options = new ChatOptions
        {
            ModelId = "o4-mini",
            ReasoningEffort = ReasoningEffort.Medium
        };

        var response = await chat.GetResponseAsync(messages, options);

        var text = response.Text;

        Assert.Contains("48 years", text);
        // NOTE: the chat client was requested as grok-3 but the chat options wanted a 
        // different model and the grok client honors that choice.
        Assert.StartsWith("o4-mini", response.ModelId);

        // Reasoning should have been set to medium
        Assert.All(requests, x =>
        {
            var search = Assert.IsType<JsonObject>(x["reasoning"]);
            Assert.Equal("medium", search["effort"]?.GetValue<string>());
        });
    }
}
