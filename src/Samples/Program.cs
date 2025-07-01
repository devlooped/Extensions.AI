// Sample X.AI client usage with .NET
var messages = new Chat()
{
    { "system", "You are an AI assistant that knows how to search the web." },
    { "user", "What's Tesla stock worth today? Search X and the news for latest info." },
};

var grok = new GrokClient(Env.Get("XAI_API_KEY")!, new GrokClientOptions()
    .UseJsonConsoleLogging(new() { WrapLength = 80 }));

var options = new ChatOptions
{
    ModelId = "grok-3",
    // Enables Live Search
    Tools = [new HostedWebSearchTool()]
};

var response = await grok.GetResponseAsync(messages, options);

AnsiConsole.MarkupLine($":robot: {response.Text.EscapeMarkup()}");