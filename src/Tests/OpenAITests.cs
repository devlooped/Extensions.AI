using System.ClientModel;
using System.Text.Json.Nodes;
using Devlooped.Extensions.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Responses;
using static ConfigurationExtensions;

namespace Devlooped.Extensions.AI;

public class OpenAITests(ITestOutputHelper output)
{
    [SecretsFact("OPENAI_API_KEY")]
    public void CanGetAsIChatClient()
    {
        var inner = new OpenAIClient(new ApiKeyCredential(Configuration["OPENAI_API_KEY"]!),
            new OpenAIClientOptions
            {
                //Endpoint = new Uri("https://api.x.ai/v1"),
            }).GetChatClient("grok-4").AsIChatClient();

        Assert.NotNull(inner);
    }

    [SecretsFact("OPENAI_API_KEY")]
    public async Task OpenAISwitchesModel()
    {
        var messages = new Chat()
        {
            { "user", "What products does Tesla make?" },
        };

        var chat = new OpenAIChatClient(Configuration["OPENAI_API_KEY"]!, "gpt-4.1-nano",
            OpenAIClientOptions.WriteTo(output));

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

        var chat = new OpenAIChatClient(Configuration["OPENAI_API_KEY"]!, "o3-mini",
            OpenAIClientOptions.Observable(requests.Add).WriteTo(output));

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

    [SecretsFact("OPENAI_API_KEY")]
    public async Task GPT5_ThinksFast()
    {
        var messages = new Chat()
        {
            { "system", "You are an intelligent AI assistant that's an expert on financial matters." },
            { "user", "If you have a debt of 100k and accumulate a compounding 5% debt on top of it every year, how long before you are a negative millonaire? (round up to full integer value)" },
        };

        var requests = new List<JsonNode>();

        var chat = new OpenAIChatClient(Configuration["OPENAI_API_KEY"]!, "gpt-5-nano",
            OpenAIClientOptions.Observable(requests.Add).WriteTo(output));

        var options = new ChatOptions
        {
            ModelId = "gpt-5",
            ReasoningEffort = ReasoningEffort.Minimal
        };

        var response = await chat.GetResponseAsync(messages, options);

        var text = response.Text;

        Assert.Contains("48 years", text);
        // NOTE: the chat client was requested as grok-3 but the chat options wanted a 
        // different model and the grok client honors that choice.
        Assert.StartsWith("gpt-5", response.ModelId);
        Assert.DoesNotContain("nano", response.ModelId);

        // Reasoning should have been set to medium
        Assert.All(requests, x =>
        {
            var search = Assert.IsType<JsonObject>(x["reasoning"]);
            Assert.Equal("minimal", search["effort"]?.GetValue<string>());
        });
    }

    [SecretsTheory("OPENAI_API_KEY")]
    [InlineData(ReasoningEffort.Minimal, "gpt-5")] // Obsolete as of 5.1
    [InlineData(ReasoningEffort.None)]
    [InlineData(ReasoningEffort.Low)]
    [InlineData(ReasoningEffort.Medium)]
    [InlineData(ReasoningEffort.High)]
    [InlineData(ReasoningEffort.XHigh)]
    public async Task GPT5_ThinkingTime(ReasoningEffort effort, string modelId = "gpt-5.2")
    {
        var messages = new Chat()
        {
            { "system", "You are an intelligent AI assistant that's an expert on financial matters." },
            { "user", "If you have a debt of 100k and accumulate a compounding 5% debt on top of it every year, how long before you are a negative millonaire? (round up to full integer value)" },
        };

        var requests = new List<JsonNode>();

        var chat = new OpenAIChatClient(Configuration["OPENAI_API_KEY"]!, modelId,
            OpenAIClientOptions.Observable(requests.Add).WriteTo(output));

        var options = new ChatOptions
        {
            ReasoningEffort = effort
        };

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var response = await chat.GetResponseAsync(messages, options);
        watch.Stop();

        var text = response.Text;

        Assert.Contains("48 years", text);
        // NOTE: the chat client was requested as grok-3 but the chat options wanted a 
        // different model and the grok client honors that choice.
        Assert.StartsWith("gpt-5", response.ModelId);
        Assert.DoesNotContain("nano", response.ModelId);

        // Reasoning should have been set to expected value
        Assert.All(requests, x =>
        {
            var search = Assert.IsType<JsonObject>(x["reasoning"]);
            Assert.Equal(effort.ToString().ToLowerInvariant(), search["effort"]?.GetValue<string>());
        });

        output.WriteLine($"Effort: {effort}, Time: {watch.ElapsedMilliseconds}ms, Tokens: {response.Usage?.TotalTokenCount}");
    }

    [SecretsFact("OPENAI_API_KEY")]
    public async Task GPT5_NoReasoningTokens()
    {
        var requests = new List<JsonNode>();

        //var chat = new OpenAIChatClient(Configuration["OPENAI_API_KEY"]!, "gpt-4o",
        //    OpenAIClientOptions.Observable(requests.Add).WriteTo(output));

        var chat = new OpenAIClient(new ApiKeyCredential(Configuration["OPENAI_API_KEY"]!),
            OpenAIClientOptions.Observable(requests.Add).WriteTo(output))
            .GetOpenAIResponseClient("gpt-4o")
            .AsIChatClient();

        var reasoned = await chat.GetResponseAsync(
            "How much gold would it take to coat the Statue of Liberty in a 1mm layer?",
            new ChatOptions
            {
                ModelId = "gpt-5.1",
                ReasoningEffort = ReasoningEffort.Low
            });

        Assert.StartsWith("gpt-5.1", reasoned.ModelId);
        Assert.NotNull(reasoned.Usage?.AdditionalCounts);
        Assert.True(reasoned.Usage.AdditionalCounts.ContainsKey("OutputTokenDetails.ReasoningTokenCount"));
        Assert.True(reasoned.Usage.AdditionalCounts["OutputTokenDetails.ReasoningTokenCount"] > 0);

        var nonreasoned = await chat.GetResponseAsync(
            "How much gold would it take to coat the Statue of Liberty in a 1mm layer?",
            new ChatOptions
            {
                ModelId = "gpt-5.1",
                ReasoningEffort = ReasoningEffort.None
            });

        Assert.NotNull(nonreasoned.Usage?.AdditionalCounts);
        Assert.True(nonreasoned.Usage.AdditionalCounts.ContainsKey("OutputTokenDetails.ReasoningTokenCount"));
        Assert.True(nonreasoned.Usage.AdditionalCounts["OutputTokenDetails.ReasoningTokenCount"] == 0);
    }

    [SecretsTheory("OPENAI_API_KEY")]
    [InlineData(Verbosity.Low)]
    [InlineData(Verbosity.Medium)]
    [InlineData(Verbosity.High)]
    public async Task GPT5_Verbosity(Verbosity verbosity)
    {
        var messages = new Chat()
        {
            { "system", "You are an intelligent AI assistant that's an expert on everything." },
            { "user", "What's the answer to the universe and everything?" },
        };

        var requests = new List<JsonNode>();

        var chat = new OpenAIChatClient(Configuration["OPENAI_API_KEY"]!, "gpt-5-nano",
            OpenAIClientOptions.Observable(requests.Add).WriteTo(output));

        var options = new ChatOptions
        {
            ModelId = "gpt-5-mini",
            Verbosity = verbosity
        };

        var watch = System.Diagnostics.Stopwatch.StartNew();
        var response = await chat.GetResponseAsync(messages, options);
        watch.Stop();

        var text = response.Text;
        output.WriteLine(text);

        Assert.StartsWith("gpt-5", response.ModelId);
        Assert.DoesNotContain("nano", response.ModelId);

        // Verbosity should have been set to the expected value
        Assert.All(requests, x =>
        {
            var text = Assert.IsType<JsonObject>(x["text"]);
            Assert.Equal(verbosity.ToString().ToLowerInvariant(), text["verbosity"]?.GetValue<string>());
        });

        output.WriteLine($"Verbosity: {verbosity}, Time: {watch.ElapsedMilliseconds}ms, Tokens: {response.Usage?.TotalTokenCount}");
    }

    [SecretsFact("OPENAI_API_KEY")]
    public async Task WebSearchCountryHighContext()
    {
        var messages = new Chat()
        {
            { "system", "Sos un asistente del Cerro Catedral, usas la funcionalidad de Live Search en el sitio oficial." },
            { "system", $"Hoy es {DateTime.Now.ToString("o")}." },
            { "system",
                """
                Web search sources: 
                https://catedralaltapatagonia.com/parte-de-nieve/
                https://catedralaltapatagonia.com/tarifas/
                https://catedralaltapatagonia.com/

                DO NOT USE https://partediario.catedralaltapatagonia.com/partediario for web search, it's **OBSOLETE**.
                """},
            { "user", "Cuanto cuesta el pase diario en el Catedral hoy?" },
        };

        var requests = new List<JsonNode>();
        var responses = new List<JsonNode>();

        var chat = new OpenAIChatClient(Configuration["OPENAI_API_KEY"]!, "gpt-4.1",
            OpenAIClientOptions.Observable(requests.Add, responses.Add).WriteTo(output));

        var options = new ChatOptions
        {
            Tools = [new Devlooped.Extensions.AI.OpenAI.WebSearchTool("AR")
            {
                Region = "Bariloche",
                TimeZone = "America/Argentina/Buenos_Aires",
            }]
        };

        var response = await chat.GetResponseAsync(messages, options);
        var text = response.Text;

        var raw = Assert.IsType<OpenAIResponse>(response.RawRepresentation);
        Assert.NotEmpty(raw.OutputItems.OfType<WebSearchCallResponseItem>());

        var assistant = raw.OutputItems.OfType<MessageResponseItem>().Where(x => x.Role == MessageRole.Assistant).FirstOrDefault();
        Assert.NotNull(assistant);

        var content = Assert.Single(assistant.Content);
        Assert.NotEmpty(content.OutputTextAnnotations);
        //Assert.Contains(content.OutputTextAnnotations,
        //    x => x.Kind == ResponseMessageAnnotationKind.UriCitation &&
        //        x.UriCitationUri.AbsoluteUri.StartsWith("https://catedralaltapatagonia.com/tarifas/"));
    }
}
