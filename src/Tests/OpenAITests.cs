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

        var chat = new OpenAIClient(new ApiKeyCredential(Configuration["OPENAI_API_KEY"]!),
            OpenAIClientOptions.WriteTo(output)).GetResponsesClient().AsIChatClient("gpt-4.1-nano");

        var options = new ChatOptions
        {
            ModelId = "gpt-4.1-mini",
        };

        var response = await chat.GetResponseAsync(messages, options);

        // NOTE: the chat client was requested as grok-3 but the chat options wanted a 
        // different model and the client honors that choice.
        Assert.StartsWith("gpt-4.1-mini", response.ModelId);
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

        var chat = new OpenAIClient(new ApiKeyCredential(Configuration["OPENAI_API_KEY"]!),
            OpenAIClientOptions.Observable(requests.Add).WriteTo(output)).GetResponsesClient().AsIChatClient("gpt-5-nano");

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

        var chat = new OpenAIClient(new ApiKeyCredential(Configuration["OPENAI_API_KEY"]!),
            OpenAIClientOptions.Observable(requests.Add, responses.Add).WriteTo(output)).GetResponsesClient().AsIChatClient("gpt-4.1");

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

        var raw = Assert.IsType<ResponseResult>(response.RawRepresentation);
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
