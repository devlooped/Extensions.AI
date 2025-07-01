// Sample X.AI client usage with .NET
var messages = new Chat()
{
    { "system", "You are a highly intelligent AI assistant." },
    { "user", "What is 101*3?" },
};

var grok = new GrokClient(Env.Get("XAI_API_KEY")!);

var options = new GrokChatOptions
{
    ModelId = "grok-3-mini", // or "grok-3-mini-fast"
    Temperature = 0.7f,
    ReasoningEffort = ReasoningEffort.High, // or GrokReasoningEffort.Low
    Search = GrokSearch.Auto, // or GrokSearch.On or GrokSearch.Off
};

var response = await grok.GetResponseAsync(messages, options);

AnsiConsole.MarkupLine($":robot: {response.Text}");